using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class Patient : Entity
{
    private Patient()
    {
    }

    private Patient(
        string name,
        string? cpf,
        DateOnly? birthDate,
        string? sex,
        string? phone,
        string? address,
        string? notes,
        IReadOnlyList<string>? comorbidities)
    {
        Name = NormalizeRequired(name, nameof(name));
        Cpf = Normalize(cpf);
        BirthDate = birthDate;
        Sex = Normalize(sex);
        Phone = Normalize(phone);
        Address = Normalize(address);
        Notes = Normalize(notes);
        Comorbidities = NormalizeList(comorbidities);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; } = string.Empty;

    public string? Cpf { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public string? Sex { get; private set; }

    public string? Phone { get; private set; }

    public string? Address { get; private set; }

    public string? Notes { get; private set; }

    public string? Comorbidities { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Patient Create(
        string name,
        string? cpf,
        DateOnly? birthDate,
        string? sex,
        string? phone,
        string? address,
        string? notes,
        IReadOnlyList<string>? comorbidities)
    {
        return new Patient(name, cpf, birthDate, sex, phone, address, notes, comorbidities);
    }

    public void Update(
        string name,
        string? cpf,
        DateOnly? birthDate,
        string? sex,
        string? phone,
        string? address,
        string? notes,
        IReadOnlyList<string>? comorbidities,
        bool isActive)
    {
        Name = NormalizeRequired(name, nameof(name));
        Cpf = Normalize(cpf);
        BirthDate = birthDate;
        Sex = Normalize(sex);
        Phone = Normalize(phone);
        Address = Normalize(address);
        Notes = Normalize(notes);
        Comorbidities = NormalizeList(comorbidities);
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Nome e obrigatorio.", paramName);
        }

        return value.Trim();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        string[] normalized = values
            .Select(Normalize)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.Length == 0 ? null : string.Join("|", normalized);
    }
}
