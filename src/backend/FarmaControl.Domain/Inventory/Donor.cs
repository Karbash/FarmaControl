using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Inventory;

public sealed class Donor : Entity
{
    private Donor()
    {
    }

    private Donor(string name, string? phone, string? notes)
    {
        Name = NormalizeRequired(name);
        Phone = Normalize(phone);
        Notes = Normalize(notes);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Donor Create(string name, string? phone, string? notes)
    {
        return new Donor(name, phone, notes);
    }

    public void Update(string name, string? phone, string? notes)
    {
        Name = NormalizeRequired(name);
        Phone = Normalize(phone);
        Notes = Normalize(notes);
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
