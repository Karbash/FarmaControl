namespace FarmaControl.Application.Abstractions;

public interface IInitialDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
