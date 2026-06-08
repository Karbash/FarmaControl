using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class MedicalRecord : Entity
{
    private MedicalRecord()
    {
    }

    private MedicalRecord(
        long appointmentId,
        long patientId,
        string? doctorName,
        string? anamnesis,
        string? physicalExam,
        string? diagnosticHypothesis,
        string? cid10,
        string? conduct,
        string? notes)
    {
        if (appointmentId <= 0)
        {
            throw new ArgumentException("Atendimento e obrigatorio.", nameof(appointmentId));
        }

        if (patientId <= 0)
        {
            throw new ArgumentException("Paciente e obrigatorio.", nameof(patientId));
        }

        AppointmentId = appointmentId;
        PatientId = patientId;
        Update(doctorName, anamnesis, physicalExam, diagnosticHypothesis, cid10, conduct, notes);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public long AppointmentId { get; private set; }
    public long PatientId { get; private set; }
    public string? DoctorName { get; private set; }
    public string? Anamnesis { get; private set; }
    public string? PhysicalExam { get; private set; }
    public string? DiagnosticHypothesis { get; private set; }
    public string? Cid10 { get; private set; }
    public string? Conduct { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static MedicalRecord Create(
        long appointmentId,
        long patientId,
        string? doctorName,
        string? anamnesis,
        string? physicalExam,
        string? diagnosticHypothesis,
        string? cid10,
        string? conduct,
        string? notes)
    {
        return new MedicalRecord(appointmentId, patientId, doctorName, anamnesis, physicalExam, diagnosticHypothesis, cid10, conduct, notes);
    }

    public void Update(
        string? doctorName,
        string? anamnesis,
        string? physicalExam,
        string? diagnosticHypothesis,
        string? cid10,
        string? conduct,
        string? notes)
    {
        DoctorName = Normalize(doctorName);
        Anamnesis = Normalize(anamnesis);
        PhysicalExam = Normalize(physicalExam);
        DiagnosticHypothesis = Normalize(diagnosticHypothesis);
        Cid10 = Normalize(cid10);
        Conduct = Normalize(conduct);
        Notes = Normalize(notes);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
