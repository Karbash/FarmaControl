using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalAttendances.Models;

public sealed record UpdateMedicalAttendanceModel(
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

        errors.AddRange(VitalSigns.Validate());
        errors.AddRange(Prescriptions.SelectMany(item => item.Validate()));
        errors.AddRange(NursingChecks.SelectMany(item => item.Validate()));
        errors.AddRange(Dispensations.SelectMany(item => item.Validate()));
        errors.AddRange(Cid10Codes.SelectMany(item => item.Validate()));

        return errors;
    }

    public void ApplyTo(MedicalAttendance attendance)
    {
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
    }

    public static UpdateMedicalAttendanceModel FromRequest(UpdateMedicalAttendanceRequest request)
    {
        return new UpdateMedicalAttendanceModel(
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

}
