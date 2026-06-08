using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Triage.Abstractions;

public interface ITriageRecordRepository
{
    Task<IReadOnlyList<TriageRecord>> ListByAppointmentAsync(long appointmentId, CancellationToken cancellationToken);
    Task<TriageRecord?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task AddAsync(TriageRecord triage, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
