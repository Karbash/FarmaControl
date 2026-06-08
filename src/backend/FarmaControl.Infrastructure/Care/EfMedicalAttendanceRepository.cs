using FarmaControl.Application.Care.MedicalAttendances.Abstractions;
using FarmaControl.Domain.Care;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Care;

public sealed class EfMedicalAttendanceRepository(FarmaControlDbContext dbContext)
    : IMedicalAttendanceRepository
{
    public Task<MedicalAttendance?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return MedicalAttendancesWithItems()
            .FirstOrDefaultAsync(attendance => attendance.Id == id, cancellationToken);
    }

    public Task<MedicalAttendance?> GetByAppointmentIdAsync(
        long appointmentId,
        CancellationToken cancellationToken)
    {
        return MedicalAttendancesWithItems()
            .FirstOrDefaultAsync(attendance => attendance.AppointmentId == appointmentId, cancellationToken);
    }

    public Task<bool> ExistsForAppointmentAsync(long appointmentId, CancellationToken cancellationToken)
    {
        return dbContext.MedicalAttendances.AnyAsync(
            attendance => attendance.AppointmentId == appointmentId,
            cancellationToken);
    }

    public async Task AddAsync(MedicalAttendance attendance, CancellationToken cancellationToken)
    {
        await dbContext.MedicalAttendances.AddAsync(attendance, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<MedicalAttendance> MedicalAttendancesWithItems()
    {
        return dbContext.MedicalAttendances
            .AsSplitQuery()
            .Include(attendance => attendance.Prescriptions)
            .Include(attendance => attendance.NursingChecks)
            .Include(attendance => attendance.Dispensations)
            .Include(attendance => attendance.Cid10Codes);
    }
}
