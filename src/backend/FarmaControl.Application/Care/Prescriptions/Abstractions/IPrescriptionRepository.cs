using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Prescriptions.Abstractions;

public interface IPrescriptionRepository
{
    Task<IReadOnlyList<Prescription>> ListAsync(
        long? medicalRecordId,
        long? patientId,
        bool? isDispensed,
        CancellationToken cancellationToken);

    Task<Prescription?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task AddAsync(Prescription prescription, CancellationToken cancellationToken);

    void Remove(Prescription prescription);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
