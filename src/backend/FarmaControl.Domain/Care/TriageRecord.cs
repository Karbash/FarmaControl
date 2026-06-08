using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class TriageRecord : Entity
{
    private TriageRecord()
    {
    }

    private TriageRecord(
        long appointmentId,
        string? bloodPressure,
        string? temperature,
        string? weight,
        string? height,
        string? heartRate,
        string? oxygenSaturation,
        string? chiefComplaint,
        string? responsible,
        string? notes)
    {
        if (appointmentId <= 0)
        {
            throw new ArgumentException("Atendimento e obrigatorio.", nameof(appointmentId));
        }

        AppointmentId = appointmentId;
        Update(bloodPressure, temperature, weight, height, heartRate, oxygenSaturation, chiefComplaint, responsible, notes);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public long AppointmentId { get; private set; }
    public string? BloodPressure { get; private set; }
    public string? Temperature { get; private set; }
    public string? Weight { get; private set; }
    public string? Height { get; private set; }
    public string? HeartRate { get; private set; }
    public string? OxygenSaturation { get; private set; }
    public string? ChiefComplaint { get; private set; }
    public string? Responsible { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static TriageRecord Create(
        long appointmentId,
        string? bloodPressure,
        string? temperature,
        string? weight,
        string? height,
        string? heartRate,
        string? oxygenSaturation,
        string? chiefComplaint,
        string? responsible,
        string? notes)
    {
        return new TriageRecord(appointmentId, bloodPressure, temperature, weight, height, heartRate, oxygenSaturation, chiefComplaint, responsible, notes);
    }

    public void Update(
        string? bloodPressure,
        string? temperature,
        string? weight,
        string? height,
        string? heartRate,
        string? oxygenSaturation,
        string? chiefComplaint,
        string? responsible,
        string? notes)
    {
        BloodPressure = Normalize(bloodPressure);
        Temperature = Normalize(temperature);
        Weight = Normalize(weight);
        Height = Normalize(height);
        HeartRate = Normalize(heartRate);
        OxygenSaturation = Normalize(oxygenSaturation);
        ChiefComplaint = Normalize(chiefComplaint);
        Responsible = Normalize(responsible);
        Notes = Normalize(notes);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
