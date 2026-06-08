using FarmaControl.Application.Care.Patients.Abstractions;
using FarmaControl.Domain.Care;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Care;

public sealed class EfPatientRepository(FarmaControlDbContext dbContext) : IPatientRepository
{
    public async Task<IReadOnlyList<Patient>> ListAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        IQueryable<Patient> query = dbContext.Patients.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(patient => patient.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(patient =>
                patient.Name.Contains(term) ||
                (patient.Cpf != null && patient.Cpf.Contains(term)) ||
                (patient.Phone != null && patient.Phone.Contains(term)));
        }

        return await query.OrderBy(patient => patient.Name).ToArrayAsync(cancellationToken);
    }

    public Task<Patient?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext.Patients.FirstOrDefaultAsync(patient => patient.Id == id, cancellationToken);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken)
    {
        await dbContext.Patients.AddAsync(patient, cancellationToken);
    }

    public void Remove(Patient patient)
    {
        dbContext.Patients.Remove(patient);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
