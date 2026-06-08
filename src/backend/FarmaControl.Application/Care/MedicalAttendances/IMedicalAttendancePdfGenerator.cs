using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalAttendances;

public interface IMedicalAttendancePdfGenerator
{
    Task<byte[]> GenerateAsync(
        MedicalAttendance attendance,
        CancellationToken cancellationToken);
}
