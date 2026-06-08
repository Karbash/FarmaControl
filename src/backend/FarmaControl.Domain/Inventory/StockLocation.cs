using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Inventory;

public sealed class StockLocation : Entity
{
    private StockLocation()
    {
    }

    private StockLocation(string name)
    {
        Name = NormalizeRequired(name);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static StockLocation Create(string name)
    {
        return new StockLocation(name);
    }

    public void Update(string name)
    {
        Name = NormalizeRequired(name);
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Nome e obrigatorio.", nameof(value));
        }

        return value.Trim();
    }
}
