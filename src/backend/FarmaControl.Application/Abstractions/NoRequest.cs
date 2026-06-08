namespace FarmaControl.Application.Abstractions;

public sealed record NoRequest
{
    public static readonly NoRequest Instance = new();

    private NoRequest()
    {
    }
}
