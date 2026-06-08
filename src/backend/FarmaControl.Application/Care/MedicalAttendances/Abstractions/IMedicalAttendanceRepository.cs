using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalAttendances.Abstractions;

public interface IMedicalAttendanceRepository
{
    Task<MedicalAttendance?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<MedicalAttendance?> GetByAppointmentIdAsync(long appointmentId, CancellationToken cancellationToken);

    Task<bool> ExistsForAppointmentAsync(long appointmentId, CancellationToken cancellationToken);

    Task AddAsync(MedicalAttendance attendance, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
