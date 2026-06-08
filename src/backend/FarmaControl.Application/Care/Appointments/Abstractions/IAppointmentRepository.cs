using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Appointments.Abstractions;

public interface IAppointmentRepository
{
    Task<IReadOnlyList<CareAppointment>> ListAsync(
        DateOnly? date,
        string? status,
        long? patientId,
        CancellationToken cancellationToken);

    Task<CareAppointment?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task AddAsync(CareAppointment appointment, CancellationToken cancellationToken);

    void Remove(CareAppointment appointment);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
