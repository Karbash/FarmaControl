namespace FarmaControl.Domain.Care;

public sealed record AppointmentStatus
{
    public static readonly AppointmentStatus Waiting = new("aguardando");
    public static readonly AppointmentStatus Triage = new("triagem");
    public static readonly AppointmentStatus InCare = new("em_atendimento");
    public static readonly AppointmentStatus Dispensation = new("dispensacao");
    public static readonly AppointmentStatus Closed = new("encerrado");
    public static readonly AppointmentStatus Cancelled = new("cancelado");

    private static readonly IReadOnlyDictionary<string, AppointmentStatus> Values =
        new[]
        {
            Waiting,
            Triage,
            InCare,
            Dispensation,
            Closed,
            Cancelled
        }.ToDictionary(status => status.Value, StringComparer.OrdinalIgnoreCase);

    private AppointmentStatus(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static AppointmentStatus From(string value)
    {
        if (Values.TryGetValue(value, out AppointmentStatus? status))
        {
            return status;
        }

        throw new ArgumentException("Status de atendimento invalido.", nameof(value));
    }

    public override string ToString() => Value;
}
