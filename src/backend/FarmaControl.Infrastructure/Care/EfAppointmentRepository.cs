using FarmaControl.Application.Care.Appointments.Abstractions;
using FarmaControl.Domain.Care;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Care;

public sealed class EfAppointmentRepository(FarmaControlDbContext dbContext) : IAppointmentRepository
{
    public async Task<IReadOnlyList<CareAppointment>> ListAsync(
        DateOnly? date,
        string? status,
        long? patientId,
        CancellationToken cancellationToken)
    {
        IQueryable<CareAppointment> query = dbContext.CareAppointments.AsNoTracking();

        if (date.HasValue)
        {
            query = query.Where(appointment => appointment.Date == date.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            AppointmentStatus appointmentStatus = AppointmentStatus.From(status);
            query = query.Where(appointment => appointment.Status == appointmentStatus);
        }

        if (patientId.HasValue)
        {
            query = query.Where(appointment => appointment.PatientId == patientId.Value);
        }

        return await query
            .OrderByDescending(appointment => appointment.Date)
            .ThenByDescending(appointment => appointment.Time)
            .ToArrayAsync(cancellationToken);
    }

    public Task<CareAppointment?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext.CareAppointments.FirstOrDefaultAsync(
            appointment => appointment.Id == id,
            cancellationToken);
    }

    public async Task AddAsync(CareAppointment appointment, CancellationToken cancellationToken)
    {
        await dbContext.CareAppointments.AddAsync(appointment, cancellationToken);
    }

    public void Remove(CareAppointment appointment)
    {
        dbContext.CareAppointments.Remove(appointment);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
