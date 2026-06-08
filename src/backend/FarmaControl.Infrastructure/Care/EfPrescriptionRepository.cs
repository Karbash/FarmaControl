using FarmaControl.Application.Care.Prescriptions.Abstractions;
using FarmaControl.Domain.Care;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Care;

public sealed class EfPrescriptionRepository(FarmaControlDbContext dbContext) : IPrescriptionRepository
{
    public async Task<IReadOnlyList<Prescription>> ListAsync(
        long? medicalRecordId,
        long? patientId,
        bool? isDispensed,
        CancellationToken cancellationToken)
    {
        IQueryable<Prescription> query = dbContext.Prescriptions.AsNoTracking();

        if (medicalRecordId.HasValue)
        {
            query = query.Where(prescription => prescription.MedicalRecordId == medicalRecordId.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(prescription => prescription.PatientId == patientId.Value);
        }

        if (isDispensed.HasValue)
        {
            query = query.Where(prescription => prescription.IsDispensed == isDispensed.Value);
        }

        return await query.OrderByDescending(prescription => prescription.Id).ToArrayAsync(cancellationToken);
    }

    public Task<Prescription?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext.Prescriptions.FirstOrDefaultAsync(prescription => prescription.Id == id, cancellationToken);
    }

    public async Task AddAsync(Prescription prescription, CancellationToken cancellationToken)
    {
        await dbContext.Prescriptions.AddAsync(prescription, cancellationToken);
    }

    public void Remove(Prescription prescription)
    {
        dbContext.Prescriptions.Remove(prescription);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
