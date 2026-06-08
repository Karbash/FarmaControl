using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalRecords.Abstractions;

public interface IMedicalRecordRepository
{
    Task<IReadOnlyList<MedicalRecord>> ListAsync(long? appointmentId, long? patientId, CancellationToken cancellationToken);
    Task<MedicalRecord?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task AddAsync(MedicalRecord record, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
