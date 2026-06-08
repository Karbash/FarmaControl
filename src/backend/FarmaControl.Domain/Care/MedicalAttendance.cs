using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class MedicalAttendance : Entity
{
    private readonly List<MedicalAttendancePrescriptionItem> prescriptions = [];
    private readonly List<MedicalAttendanceNursingCheckItem> nursingChecks = [];
    private readonly List<MedicalAttendanceDispensationItem> dispensations = [];
    private readonly List<MedicalAttendanceCid10Item> cid10Codes = [];

    private MedicalAttendance()
    {
    }

    private MedicalAttendance(
        long appointmentId,
        long patientId,
        long? responsibleUserId,
        string? responsibleName,
        string name,
        int? age,
        DateOnly attendanceDate,
        TimeOnly? attendanceTime,
        string? city,
        string? church,
        string? pastor,
        AttendanceType attendanceType,
        int? returnNumber)
    {
        AppointmentId = appointmentId;
        PatientId = patientId;
        ResponsibleUserId = responsibleUserId;
        ResponsibleName = responsibleName;
        Name = name;
        Age = age;
        AttendanceDate = attendanceDate;
        AttendanceTime = attendanceTime;
        City = city;
        Church = church;
        Pastor = pastor;
        AttendanceType = attendanceType;
        ReturnNumber = returnNumber;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public long AppointmentId { get; private set; }

    public long PatientId { get; private set; }

    public long? ResponsibleUserId { get; private set; }

    public string? ResponsibleName { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int? Age { get; private set; }

    public DateOnly AttendanceDate { get; private set; }

    public TimeOnly? AttendanceTime { get; private set; }

    public string? City { get; private set; }

    public string? Church { get; private set; }

    public string? Pastor { get; private set; }

    public AttendanceType AttendanceType { get; private set; }

    public int? ReturnNumber { get; private set; }

    public VitalSigns VitalSigns { get; private set; } = VitalSigns.Create(null, null, null, null, null, null);

    public string? ChiefComplaint { get; private set; }

    public string? PreviousPathologicalHistory { get; private set; }

    public string? CurrentDiseaseHistory { get; private set; }

    public string? PhysicalExam { get; private set; }

    public string? DiagnosticHypothesis { get; private set; }

    public string? Cid10Code { get; private set; }

    public string? Cid10Name { get; private set; }

    public string? ResponsibleSignature { get; private set; }

    public long? TriageResponsibleUserId { get; private set; }

    public string? TriageResponsibleName { get; private set; }

    public string? TriageResponsibleSignature { get; private set; }

    public long? MedicalResponsibleUserId { get; private set; }

    public string? MedicalResponsibleName { get; private set; }

    public string? MedicalResponsibleSignature { get; private set; }

    public long? DispensationResponsibleUserId { get; private set; }

    public string? DispensationResponsibleName { get; private set; }

    public string? DispensationResponsibleSignature { get; private set; }

    public bool HasBackSide { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<MedicalAttendancePrescriptionItem> Prescriptions => prescriptions.AsReadOnly();

    public IReadOnlyCollection<MedicalAttendanceNursingCheckItem> NursingChecks => nursingChecks.AsReadOnly();

    public IReadOnlyCollection<MedicalAttendanceDispensationItem> Dispensations => dispensations.AsReadOnly();

    public IReadOnlyCollection<MedicalAttendanceCid10Item> Cid10Codes => cid10Codes.AsReadOnly();

    public static MedicalAttendance Create(
        long appointmentId,
        long patientId,
        long? responsibleUserId,
        string? responsibleName,
        string name,
        int? age,
        DateOnly attendanceDate,
        TimeOnly? attendanceTime,
        string? city,
        string? church,
        string? pastor,
        AttendanceType attendanceType,
        int? returnNumber)
    {
        if (appointmentId <= 0)
        {
            throw new ArgumentException("Atendimento e obrigatorio.", nameof(appointmentId));
        }

        if (patientId <= 0)
        {
            throw new ArgumentException("Paciente e obrigatorio.", nameof(patientId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nome e obrigatorio.", nameof(name));
        }

        return new MedicalAttendance(
            appointmentId,
            patientId,
            responsibleUserId,
            responsibleName,
            name.Trim(),
            age,
            attendanceDate,
            attendanceTime,
            city,
            church,
            pastor,
            attendanceType,
            returnNumber);
    }

    public void UpdateClinicalHistory(
        string? chiefComplaint,
        string? previousPathologicalHistory,
        string? currentDiseaseHistory,
        string? physicalExam,
        string? diagnosticHypothesis = null,
        string? cid10Code = null,
        string? cid10Name = null)
    {
        ChiefComplaint = chiefComplaint;
        PreviousPathologicalHistory = previousPathologicalHistory;
        CurrentDiseaseHistory = currentDiseaseHistory;
        PhysicalExam = physicalExam;
        DiagnosticHypothesis = Normalize(diagnosticHypothesis);
        Cid10Code = Normalize(cid10Code);
        Cid10Name = Normalize(cid10Name);
        Touch();
    }

    public void UpdateVitalSigns(VitalSigns vitalSigns)
    {
        VitalSigns = vitalSigns;
        Touch();
    }

    public void UpdateSignature(string? responsibleSignature, bool hasBackSide)
    {
        ResponsibleSignature = responsibleSignature;
        HasBackSide = hasBackSide;
        Touch();
    }

    public void SignTriage(long userId, string userName, string? signature)
    {
        TriageResponsibleUserId = userId;
        TriageResponsibleName = Normalize(userName);
        TriageResponsibleSignature = Normalize(signature) ?? Normalize(userName);
        UpdateLegacySignatureIfEmpty(userId, userName, signature);
        Touch();
    }

    public void SignMedical(long userId, string userName, string? signature)
    {
        MedicalResponsibleUserId = userId;
        MedicalResponsibleName = Normalize(userName);
        MedicalResponsibleSignature = Normalize(signature) ?? Normalize(userName);
        UpdateLegacySignatureIfEmpty(userId, userName, signature);
        Touch();
    }

    public void SignDispensation(long userId, string userName, string? signature)
    {
        DispensationResponsibleUserId = userId;
        DispensationResponsibleName = Normalize(userName);
        DispensationResponsibleSignature = Normalize(signature) ?? Normalize(userName);
        UpdateLegacySignatureIfEmpty(userId, userName, signature);
        Touch();
    }

    public void ReplacePrescriptions(IEnumerable<MedicalAttendancePrescriptionItem> items)
    {
        MedicalAttendancePrescriptionItem[] nextItems = items
            .OrderBy(item => item.Order)
            .Select(item => prescriptions.FirstOrDefault(current => SamePrescription(current, item)) ?? item)
            .ToArray();

        prescriptions.Clear();
        prescriptions.AddRange(nextItems);
        Touch();
    }

    public void ReplaceNursingChecks(IEnumerable<MedicalAttendanceNursingCheckItem> items)
    {
        nursingChecks.Clear();
        nursingChecks.AddRange(items.OrderBy(item => item.Order));
        Touch();
    }

    public void ReplaceDispensations(IEnumerable<MedicalAttendanceDispensationItem> items)
    {
        MedicalAttendanceDispensationItem[] linkedItems = dispensations
            .Where(item => item.PrescriptionId.HasValue)
            .ToArray();
        MedicalAttendanceDispensationItem[] newLinkedItems = items
            .Where(item => item.PrescriptionId.HasValue)
            .Where(item => linkedItems.All(existing => existing.PrescriptionId != item.PrescriptionId))
            .OrderBy(item => item.Order)
            .ToArray();

        dispensations.Clear();
        dispensations.AddRange(linkedItems);
        dispensations.AddRange(newLinkedItems);
        dispensations.AddRange(items
            .Where(item => !item.PrescriptionId.HasValue)
            .OrderBy(item => item.Order));
        Touch();
    }

    public void ReplaceCid10Codes(IEnumerable<MedicalAttendanceCid10Item> items)
    {
        cid10Codes.Clear();
        cid10Codes.AddRange(items
            .GroupBy(item => item.Cid10CodeId)
            .Select(group => group.OrderBy(item => item.Order).First())
            .OrderBy(item => item.Order));

        MedicalAttendanceCid10Item? first = cid10Codes.FirstOrDefault();
        Cid10Code = first?.Code;
        Cid10Name = first?.Name;
        Touch();
    }

    public void AttachPrescriptionDispensation(
        long prescriptionId,
        long medicationId,
        string? medicationName,
        string? batch,
        int quantity,
        string responsible,
        DateTimeOffset dispensedAt)
    {
        if (dispensations.Any(item => item.PrescriptionId == prescriptionId))
        {
            return;
        }

        int order = dispensations.Count == 0
            ? 1
            : dispensations.Max(item => item.Order) + 1;

        dispensations.Add(MedicalAttendanceDispensationItem.CreateFromPrescription(
            order,
            batch,
            prescriptionId,
            medicationId,
            medicationName,
            quantity,
            responsible,
            dispensedAt));

        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static bool SamePrescription(
        MedicalAttendancePrescriptionItem current,
        MedicalAttendancePrescriptionItem next)
    {
        return current.Order == next.Order &&
            Same(current.Description, next.Description) &&
            current.MedicationId == next.MedicationId &&
            Same(current.MedicationName, next.MedicationName) &&
            Same(current.Dosage, next.Dosage) &&
            Same(current.Directions, next.Directions) &&
            current.Quantity == next.Quantity;
    }

    private static bool Same(string? current, string? next)
    {
        return string.Equals(current?.Trim(), next?.Trim(), StringComparison.Ordinal);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void UpdateLegacySignatureIfEmpty(long userId, string userName, string? signature)
    {
        ResponsibleUserId ??= userId;
        ResponsibleName ??= Normalize(userName);
        ResponsibleSignature ??= Normalize(signature) ?? Normalize(userName);
    }
}
