using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalAttendances.Models;

public sealed record CreateMedicalAttendanceModel(
    long AppointmentId,
    long PatientId,
    long? ResponsibleUserId,
    string? ResponsibleName,
    string Name,
    int? Age,
    DateOnly AttendanceDate,
    TimeOnly? AttendanceTime,
    string? City,
    string? Church,
    string? Pastor,
    string AttendanceType,
    int? ReturnNumber,
    VitalSignsModel VitalSigns,
    string? ChiefComplaint,
    string? PreviousPathologicalHistory,
    string? CurrentDiseaseHistory,
    string? PhysicalExam,
    string? DiagnosticHypothesis,
    string? Cid10Code,
    string? Cid10Name,
    IReadOnlyList<Cid10ItemModel> Cid10Codes,
    IReadOnlyList<PrescriptionItemModel> Prescriptions,
    IReadOnlyList<NursingCheckItemModel> NursingChecks,
    IReadOnlyList<DispensationItemModel> Dispensations,
    string? ResponsibleSignature,
    bool HasBackSide) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (AppointmentId <= 0)
        {
            errors.Add(AppError.Validation("Atendimento e obrigatorio."));
        }

        if (PatientId <= 0)
        {
            errors.Add(AppError.Validation("Paciente e obrigatorio."));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(AppError.Validation("Nome e obrigatorio."));
        }

        if (!TryParseAttendanceType(AttendanceType, out _))
        {
            errors.Add(AppError.Validation("Tipo de pessoa invalido."));
        }

        errors.AddRange(VitalSigns.Validate());
        errors.AddRange(Prescriptions.SelectMany(item => item.Validate()));
        errors.AddRange(NursingChecks.SelectMany(item => item.Validate()));
        errors.AddRange(Dispensations.SelectMany(item => item.Validate()));
        errors.AddRange(Cid10Codes.SelectMany(item => item.Validate()));

        return errors;
    }

    public MedicalAttendance ToDomain()
    {
        var attendance = MedicalAttendance.Create(
            AppointmentId,
            PatientId,
            ResponsibleUserId,
            ResponsibleName,
            Name,
            Age,
            AttendanceDate,
            AttendanceTime,
            City,
            Church,
            Pastor,
            ParseAttendanceType(AttendanceType),
            ReturnNumber);

        attendance.UpdateVitalSigns(VitalSigns.ToDomain());
        attendance.UpdateClinicalHistory(
            ChiefComplaint,
            PreviousPathologicalHistory,
            CurrentDiseaseHistory,
            PhysicalExam,
            DiagnosticHypothesis,
            Cid10Code,
            Cid10Name);
        attendance.ReplaceCid10Codes(Cid10Codes.Select(item => item.ToDomain()));
        attendance.ReplacePrescriptions(Prescriptions.Select(item => item.ToDomain()));
        attendance.ReplaceNursingChecks(NursingChecks.Select(item => item.ToDomain()));
        attendance.ReplaceDispensations(Dispensations.Select(item => item.ToDomain()));
        attendance.UpdateSignature(ResponsibleSignature, HasBackSide);

        return attendance;
    }

    public static CreateMedicalAttendanceModel FromRequest(
        long appointmentId,
        CreateMedicalAttendanceRequest request,
        long? responsibleUserId = null,
        string? responsibleName = null)
    {
        return new CreateMedicalAttendanceModel(
            appointmentId,
            request.PatientId,
            responsibleUserId ?? request.ResponsibleUserId,
            responsibleName ?? request.ResponsibleName,
            request.Name,
            request.Age,
            request.AttendanceDate,
            request.AttendanceTime,
            request.City,
            request.Church,
            request.Pastor,
            request.AttendanceType,
            request.ReturnNumber,
            VitalSignsModel.FromRequest(request.VitalSigns),
            request.ChiefComplaint,
            request.PreviousPathologicalHistory,
            request.CurrentDiseaseHistory,
            request.PhysicalExam,
            request.DiagnosticHypothesis,
            request.Cid10Code,
            request.Cid10Name,
            request.Cid10Codes?.Select(Cid10ItemModel.FromRequest).ToArray() ?? [],
            request.Prescriptions?.Select(PrescriptionItemModel.FromRequest).ToArray() ?? [],
            request.NursingChecks?.Select(NursingCheckItemModel.FromRequest).ToArray() ?? [],
            request.Dispensations?.Select(DispensationItemModel.FromRequest).ToArray() ?? [],
            request.ResponsibleSignature,
            request.HasBackSide);
    }

    private static AttendanceType ParseAttendanceType(string value)
    {
        return TryParseAttendanceType(value, out AttendanceType attendanceType)
            ? attendanceType
            : throw new InvalidOperationException("Tipo de pessoa invalido.");
    }

    private static bool TryParseAttendanceType(string value, out AttendanceType attendanceType)
    {
        string normalized = value.Trim().ToLowerInvariant();

        if (normalized is "part" or "participante")
        {
            attendanceType = global::FarmaControl.Domain.Care.AttendanceType.Participante;
            return true;
        }

        if (normalized is "trab" or "trabalhador")
        {
            attendanceType = global::FarmaControl.Domain.Care.AttendanceType.Trabalhador;
            return true;
        }

        if (normalized is "pastor")
        {
            attendanceType = global::FarmaControl.Domain.Care.AttendanceType.Pastor;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out attendanceType);
    }
}
