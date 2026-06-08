using FarmaControl.Application.Care.Triage.Abstractions;
using FarmaControl.Domain.Care;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Care;

public sealed class EfTriageRecordRepository(FarmaControlDbContext dbContext) : ITriageRecordRepository
{
    public async Task<IReadOnlyList<TriageRecord>> ListByAppointmentAsync(long appointmentId, CancellationToken cancellationToken)
    {
        return await dbContext.TriageRecords
            .Where(triage => triage.AppointmentId == appointmentId)
            .OrderByDescending(triage => triage.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<TriageRecord?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext.TriageRecords.FirstOrDefaultAsync(triage => triage.Id == id, cancellationToken);
    }

    public async Task AddAsync(TriageRecord triage, CancellationToken cancellationToken)
    {
        await dbContext.TriageRecords.AddAsync(triage, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
