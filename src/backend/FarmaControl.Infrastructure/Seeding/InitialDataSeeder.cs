using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Domain.Care;
using FarmaControl.Domain.Users;
using FarmaControl.Infrastructure.Persistence;
using FarmaControl.Infrastructure.Persistence.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmaControl.Infrastructure.Seeding;

public sealed class InitialDataSeeder(
    FarmaControlDbContext dbContext,
    IPasswordHasher passwordHasher,
    IHostEnvironment environment) : IInitialDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await ConfigureSqliteAsync(cancellationToken);

        if (!await IsLegacyEnsureCreatedDatabaseAsync(cancellationToken))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        await UserSchemaUpgrader.ApplyAsync(dbContext, cancellationToken);
        await AuditSchemaUpgrader.ApplyAsync(dbContext, cancellationToken);
        await InventorySchemaUpgrader.ApplyAsync(dbContext, cancellationToken);
        await Cid10SchemaUpgrader.ApplyAsync(dbContext, cancellationToken);
        await CareSchemaUpgrader.ApplyAsync(dbContext, cancellationToken);
        await SeedCid10Async(cancellationToken);

        bool hasUsers = await dbContext.Users.AnyAsync(cancellationToken);
        if (hasUsers)
        {
            return;
        }

        string adminPassword = ResolveInitialAdminPassword();
        var admin = User.Create(
            "Administrador",
            "admin",
            passwordHasher.Hash(adminPassword),
            UserRole.Admin);

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ConfigureSqliteAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout = 30000;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;", cancellationToken);
    }

    private async Task<bool> IsLegacyEnsureCreatedDatabaseAsync(CancellationToken cancellationToken)
    {
        bool hasUsersTable = await TableExistsAsync("users", cancellationToken);
        bool hasMigrationsHistory = await TableExistsAsync("__EFMigrationsHistory", cancellationToken);

        return hasUsersTable && !hasMigrationsHistory;
    }

    private async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        int count = await dbContext.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(1) AS Value FROM sqlite_master WHERE type = 'table' AND name = {0}",
                tableName)
            .SingleAsync(cancellationToken);

        return count > 0;
    }

    private async Task SeedCid10Async(CancellationToken cancellationToken)
    {
        string path = ResolveCid10SeedPath();

        if (!File.Exists(path))
        {
            return;
        }

        await using FileStream stream = File.OpenRead(path);
        Cid10Root? root = await JsonSerializer.DeserializeAsync<Cid10Root>(
            stream,
            cancellationToken: cancellationToken);

        if (root?.Codigos is null || root.Codigos.Count == 0)
        {
            return;
        }

        Cid10SeedEntry[] entries = root.Codigos
            .Select(NormalizeCid10Item)
            .OfType<Cid10SeedEntry>()
            .GroupBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (entries.Length == 0)
        {
            return;
        }

        await SyncCid10CatalogAsync(entries, cancellationToken);
    }

    private async Task SyncCid10CatalogAsync(
        IReadOnlyCollection<Cid10SeedEntry> entries,
        CancellationToken cancellationToken)
    {
        var desiredByCode = entries.ToDictionary(
            entry => entry.Code,
            entry => entry.Name,
            StringComparer.OrdinalIgnoreCase);
        List<Cid10Code> existing = await dbContext.Cid10Codes
            .OrderBy(code => code.Id)
            .ToListAsync(cancellationToken);

        foreach (Cid10Code code in existing)
        {
            Cid10SeedEntry normalized = NormalizeExistingCid10Code(code);
            if (!string.Equals(code.Code, normalized.Code, StringComparison.Ordinal) ||
                !string.Equals(code.Name, normalized.Name, StringComparison.Ordinal))
            {
                code.Update(normalized.Code, normalized.Name);
            }
        }

        var canonicalByCode = new Dictionary<string, Cid10Code>(StringComparer.OrdinalIgnoreCase);
        var removedIds = new HashSet<long>();

        foreach (IGrouping<string, Cid10Code> group in existing.GroupBy(
            code => code.Code,
            StringComparer.OrdinalIgnoreCase))
        {
            desiredByCode.TryGetValue(group.Key, out string? desiredName);

            Cid10Code canonical = desiredName is null
                ? group.First()
                : group.FirstOrDefault(code => string.Equals(
                    code.Name,
                    desiredName,
                    StringComparison.OrdinalIgnoreCase)) ?? group.First();

            if (desiredName is not null)
            {
                canonical.Update(canonical.Code, desiredName);
            }

            canonicalByCode[group.Key] = canonical;

            foreach (Cid10Code duplicate in group.Where(code => code.Id != canonical.Id))
            {
                await RemapCid10ReferencesAsync(
                    duplicate.Id,
                    canonical.Id,
                    cancellationToken);
                dbContext.Cid10Codes.Remove(duplicate);
                removedIds.Add(duplicate.Id);
            }
        }

        foreach (Cid10SeedEntry entry in entries)
        {
            if (!canonicalByCode.ContainsKey(entry.Code))
            {
                dbContext.Cid10Codes.Add(Cid10Code.Create(entry.Code, entry.Name));
            }
        }

        long[] referencedIds = await dbContext.Set<MedicalAttendanceCid10Item>()
            .AsNoTracking()
            .Select(item => item.Cid10CodeId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var referenced = new HashSet<long>(referencedIds);
        Cid10Code[] staleUnused = existing
            .Where(code => !removedIds.Contains(code.Id))
            .Where(code => !desiredByCode.ContainsKey(code.Code))
            .Where(code => !referenced.Contains(code.Id))
            .ToArray();

        dbContext.Cid10Codes.RemoveRange(staleUnused);
        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureCid10UniqueCodeIndexAsync(cancellationToken);
    }

    private async Task RemapCid10ReferencesAsync(
        long sourceCid10CodeId,
        long targetCid10CodeId,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "medical_attendance_cid10_codes"
            WHERE "cid10_code_id" = {sourceCid10CodeId}
              AND EXISTS (
                  SELECT 1
                  FROM "medical_attendance_cid10_codes" AS "existing"
                  WHERE "existing"."medical_attendance_id" = "medical_attendance_cid10_codes"."medical_attendance_id"
                    AND "existing"."cid10_code_id" = {targetCid10CodeId}
              );
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "medical_attendance_cid10_codes"
            SET "cid10_code_id" = {targetCid10CodeId}
            WHERE "cid10_code_id" = {sourceCid10CodeId};
            """,
            cancellationToken);
    }

    private async Task EnsureCid10UniqueCodeIndexAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DROP INDEX IF EXISTS "IX_cid10_codes_code";
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_cid10_codes_code" ON "cid10_codes" ("code");
            """,
            cancellationToken);
    }

    private static Cid10SeedEntry? NormalizeCid10Item(Cid10Item item)
    {
        string code = item.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        string name = item.Nome?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return CorrectCid10Entry(new Cid10SeedEntry(code, name));
    }

    private static Cid10SeedEntry NormalizeExistingCid10Code(Cid10Code code)
    {
        return CorrectCid10Entry(new Cid10SeedEntry(
            code.Code.Trim().ToUpperInvariant(),
            code.Name.Trim()));
    }

    private static Cid10SeedEntry CorrectCid10Entry(Cid10SeedEntry entry)
    {
        if (string.Equals(entry.Code, "J06", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(entry.Name, "Resfriado comum", StringComparison.OrdinalIgnoreCase))
        {
            return new Cid10SeedEntry("J00", "Resfriado comum");
        }

        return entry;
    }

    private string ResolveInitialAdminPassword()
    {
        string? configuredPassword = Environment.GetEnvironmentVariable("FARMACONTROL_INITIAL_ADMIN_PASSWORD");
        if (!string.IsNullOrWhiteSpace(configuredPassword))
        {
            return configuredPassword;
        }

        if (environment.IsDevelopment())
        {
            return "admin123";
        }

        throw new InvalidOperationException(
            "Defina FARMACONTROL_INITIAL_ADMIN_PASSWORD para inicializar o primeiro administrador.");
    }

    private sealed record Cid10Root([property: JsonPropertyName("codigos")] List<Cid10Item>? Codigos);

    private sealed record Cid10SeedEntry(string Code, string Name);

    private string ResolveCid10SeedPath()
    {
        string apiDataPath = Path.GetFullPath(Path.Combine(
            environment.ContentRootPath,
            "Data",
            "cid10.json"));

        if (File.Exists(apiDataPath))
        {
            return apiDataPath;
        }

        return Path.GetFullPath(Path.Combine(
            environment.ContentRootPath,
            "..",
            "..",
            "..",
            "public",
            "dados",
            "cid10.json"));
    }

    private sealed record Cid10Item(
        [property: JsonPropertyName("codigo")] string? Codigo,
        [property: JsonPropertyName("nome")] string? Nome);
}
