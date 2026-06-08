using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class CareAppointment : Entity
{
    private CareAppointment()
    {
    }

    private CareAppointment(
        long patientId,
        DateOnly date,
        TimeOnly? time,
        string type,
        bool isEmergency,
        string? responsible,
        string? notes)
    {
        if (patientId <= 0)
        {
            throw new ArgumentException("Paciente e obrigatorio.", nameof(patientId));
        }

        PatientId = patientId;
        Date = date;
        Time = time;
        Type = Normalize(type) ?? "consulta";
        IsEmergency = isEmergency;
        Status = AppointmentStatus.Waiting;
        Responsible = Normalize(responsible);
        Notes = Normalize(notes);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public long PatientId { get; private set; }

    public DateOnly Date { get; private set; }

    public TimeOnly? Time { get; private set; }

    public string Type { get; private set; } = "consulta";

    public bool IsEmergency { get; private set; }

    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Waiting;

    public string? DoctorName { get; private set; }

    public string? Responsible { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static CareAppointment Create(
        long patientId,
        DateOnly date,
        TimeOnly? time,
        string? type,
        bool isEmergency,
        string? responsible,
        string? notes)
    {
        return new CareAppointment(
            patientId,
            date,
            time,
            string.IsNullOrWhiteSpace(type) ? "consulta" : type,
            isEmergency,
            responsible,
            notes);
    }

    public void Update(string? type, string? notes, string? doctorName)
    {
        Type = Normalize(type) ?? "consulta";
        Notes = Normalize(notes);
        DoctorName = Normalize(doctorName);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeStatus(AppointmentStatus status, string? doctorName)
    {
        Status = status;

        if (doctorName is not null)
        {
            DoctorName = Normalize(doctorName);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
