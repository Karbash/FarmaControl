using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalAttendances.Models;

public static class MedicalAttendanceModel
{
    public static MedicalAttendanceResponse FromDomain(MedicalAttendance attendance)
    {
        return new MedicalAttendanceResponse(
            attendance.Id,
            attendance.AppointmentId,
            attendance.PatientId,
            attendance.ResponsibleUserId,
            attendance.ResponsibleName,
            attendance.Name,
            attendance.Age,
            attendance.AttendanceDate,
            attendance.AttendanceTime,
            attendance.City,
            attendance.Church,
            attendance.Pastor,
            attendance.AttendanceType.ToString(),
            attendance.ReturnNumber,
            VitalSignsModel.FromDomain(attendance.VitalSigns),
            attendance.ChiefComplaint,
            attendance.PreviousPathologicalHistory,
            attendance.CurrentDiseaseHistory,
            attendance.PhysicalExam,
            attendance.Prescriptions.Select(PrescriptionItemModel.FromDomain).ToArray(),
            attendance.NursingChecks.Select(NursingCheckItemModel.FromDomain).ToArray(),
            attendance.Dispensations.Select(DispensationItemModel.FromDomain).ToArray(),
            attendance.ResponsibleSignature,
            attendance.HasBackSide,
            attendance.CreatedAt,
            attendance.UpdatedAt,
            attendance.DiagnosticHypothesis,
            attendance.Cid10Code,
            attendance.Cid10Name,
            attendance.Cid10Codes
                .OrderBy(item => item.Order)
                .Select(Cid10ItemModel.FromDomain)
                .ToArray(),
            attendance.TriageResponsibleUserId,
            attendance.TriageResponsibleName,
            attendance.TriageResponsibleSignature,
            attendance.MedicalResponsibleUserId,
            attendance.MedicalResponsibleName,
            attendance.MedicalResponsibleSignature,
            attendance.DispensationResponsibleUserId,
            attendance.DispensationResponsibleName,
            attendance.DispensationResponsibleSignature);
    }
}
