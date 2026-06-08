using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class Cid10Code : Entity
{
    private Cid10Code()
    {
    }

    private Cid10Code(string code, string name)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Cid10Code Create(string code, string name)
    {
        return new Cid10Code(code, name);
    }

    public void Update(string code, string name)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
    }

    private static string NormalizeCode(string value)
    {
        return NormalizeRequired(value).ToUpperInvariant();
    }

    private static string NormalizeName(string value)
    {
        return NormalizeRequired(value);
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Codigo e nome do CID-10 sao obrigatorios.", nameof(value));
        }

        return value.Trim();
    }
}
