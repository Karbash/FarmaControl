using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Inventory;

public sealed class Manufacturer : Entity
{
    private Manufacturer()
    {
    }

    private Manufacturer(string name, string? cnpj)
    {
        Name = NormalizeRequired(name);
        Cnpj = Normalize(cnpj);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; } = string.Empty;

    public string? Cnpj { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Manufacturer Create(string name, string? cnpj)
    {
        return new Manufacturer(name, cnpj);
    }

    public void Update(string name, string? cnpj)
    {
        Name = NormalizeRequired(name);
        Cnpj = Normalize(cnpj);
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Nome e obrigatorio.", nameof(value));
        }

        return value.Trim();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
