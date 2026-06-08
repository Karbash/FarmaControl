using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Patients.Abstractions;

public interface IPatientRepository
{
    Task<IReadOnlyList<Patient>> ListAsync(string? search, bool? isActive, CancellationToken cancellationToken);

    Task<Patient?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task AddAsync(Patient patient, CancellationToken cancellationToken);

    void Remove(Patient patient);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
