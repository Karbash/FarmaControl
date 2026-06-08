using Microsoft.EntityFrameworkCore;
using FarmaControl.Domain.Audit;
using FarmaControl.Domain.Care;
using FarmaControl.Domain.Inventory;
using FarmaControl.Domain.Users;

namespace FarmaControl.Infrastructure.Persistence;

public sealed class FarmaControlDbContext(DbContextOptions<FarmaControlDbContext> options)
    : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<MedicalAttendance> MedicalAttendances => Set<MedicalAttendance>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<CareAppointment> CareAppointments => Set<CareAppointment>();

    public DbSet<TriageRecord> TriageRecords => Set<TriageRecord>();

    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();

    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    public DbSet<Cid10Code> Cid10Codes => Set<Cid10Code>();

    public DbSet<MedicalAttendanceCid10Item> MedicalAttendanceCid10Codes => Set<MedicalAttendanceCid10Item>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Medication> Medications => Set<Medication>();

    public DbSet<Donor> Donors => Set<Donor>();

    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();

    public DbSet<StockLocation> StockLocations => Set<StockLocation>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FarmaControlDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
