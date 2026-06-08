using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FarmaControl.Infrastructure.Persistence;

public sealed class DesignTimeFarmaControlDbContextFactory
    : IDesignTimeDbContextFactory<FarmaControlDbContext>
{
    public FarmaControlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FarmaControlDbContext>();
        optionsBuilder.UseSqlite("Data Source=farmacontrol-dev.db");

        return new FarmaControlDbContext(optionsBuilder.Options);
    }
}
