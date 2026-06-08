using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Care.Appointments.Abstractions;
using FarmaControl.Application.Care.Cid10;
using FarmaControl.Application.Care.MedicalAttendances;
using FarmaControl.Application.Care.MedicalAttendances.Abstractions;
using FarmaControl.Application.Care.MedicalRecords.Abstractions;
using FarmaControl.Application.Care.Patients.Abstractions;
using FarmaControl.Application.Care.Prescriptions.Abstractions;
using FarmaControl.Application.Care.Triage.Abstractions;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Infrastructure.Care;
using FarmaControl.Infrastructure.Audit;
using FarmaControl.Infrastructure.Inventory;
using FarmaControl.Infrastructure.Persistence;
using FarmaControl.Infrastructure.Seeding;
using FarmaControl.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;

namespace FarmaControl.Infrastructure;

public static class DependencyInjection
{
    private const string DefaultConnectionString = "Data Source=farmacontrol.db";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("FarmaControlDb") ?? DefaultConnectionString;
        connectionString = NormalizeSqliteConnectionString(connectionString);

        services.AddDbContext<FarmaControlDbContext>(options =>
            options.UseSqlite(
                connectionString,
                sqlite => sqlite.CommandTimeout(60)));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDatabaseHealthCheck, SqliteDatabaseHealthCheck>();
        services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
        services.AddScoped<IAuditLogger, EfAuditLogger>();
        services.AddScoped<IAuditReportPdfGenerator, SimpleAuditReportPdfGenerator>();
        services.AddScoped<IPatientRepository, EfPatientRepository>();
        services.AddScoped<IAppointmentRepository, EfAppointmentRepository>();
        services.AddScoped<ITriageRecordRepository, EfTriageRecordRepository>();
        services.AddScoped<IMedicalRecordRepository, EfMedicalRecordRepository>();
        services.AddScoped<IPrescriptionRepository, EfPrescriptionRepository>();
        services.AddScoped<IMedicalAttendanceRepository, EfMedicalAttendanceRepository>();
        services.AddScoped<IMedicalAttendancePdfGenerator, SimpleMedicalAttendancePdfGenerator>();
        services.AddScoped<ICid10Catalog, DbCid10Catalog>();
        services.AddScoped<IMedicationRepository, EfMedicationRepository>();
        services.AddScoped<IStockMovementRepository, EfStockMovementRepository>();
        services.AddScoped<IDonorRepository, EfDonorRepository>();
        services.AddScoped<IManufacturerRepository, EfManufacturerRepository>();
        services.AddScoped<IStockLocationRepository, EfStockLocationRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IInitialDataSeeder, InitialDataSeeder>();

        return services;
    }

    private static string NormalizeSqliteConnectionString(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString)
        {
            DefaultTimeout = 30,
            Pooling = false
        };

        return builder.ToString();
    }
}
