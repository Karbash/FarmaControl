using FarmaControl.Application.Care.MedicalRecords.Abstractions;
using FarmaControl.Domain.Care;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Care;

public sealed class EfMedicalRecordRepository(FarmaControlDbContext dbContext) : IMedicalRecordRepository
{
    public async Task<IReadOnlyList<MedicalRecord>> ListAsync(long? appointmentId, long? patientId, CancellationToken cancellationToken)
    {
        IQueryable<MedicalRecord> query = dbContext.MedicalRecords.AsNoTracking();

        if (appointmentId.HasValue)
        {
            query = query.Where(record => record.AppointmentId == appointmentId.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(record => record.PatientId == patientId.Value);
        }

        return await query.OrderByDescending(record => record.Id).ToArrayAsync(cancellationToken);
    }

    public Task<MedicalRecord?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext.MedicalRecords.FirstOrDefaultAsync(record => record.Id == id, cancellationToken);
    }

    public async Task AddAsync(MedicalRecord record, CancellationToken cancellationToken)
    {
        await dbContext.MedicalRecords.AddAsync(record, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
