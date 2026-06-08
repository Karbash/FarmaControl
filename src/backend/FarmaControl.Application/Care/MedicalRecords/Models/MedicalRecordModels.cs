using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalRecords.Models;

public sealed record MedicalRecordInputModel(
    long AppointmentId,
    long PatientId,
    string? DoctorName,
    string? Anamnesis,
    string? PhysicalExam,
    string? DiagnosticHypothesis,
    string? Cid10,
    string? Conduct,
    string? Notes) : IExplicitModel
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

        return errors;
    }

    public MedicalRecord ToDomain()
    {
        return MedicalRecord.Create(AppointmentId, PatientId, DoctorName, Anamnesis, PhysicalExam, DiagnosticHypothesis, Cid10, Conduct, Notes);
    }

    public void ApplyTo(MedicalRecord record)
    {
        record.Update(DoctorName, Anamnesis, PhysicalExam, DiagnosticHypothesis, Cid10, Conduct, Notes);
    }

    public static MedicalRecordInputModel FromRequest(CreateMedicalRecordRequest request)
    {
        return new MedicalRecordInputModel(request.AppointmentId, request.PatientId, request.DoctorName, request.Anamnesis, request.PhysicalExam, request.DiagnosticHypothesis, request.Cid10, request.Conduct, request.Notes);
    }

    public static MedicalRecordInputModel FromRequest(MedicalRecord existing, UpdateMedicalRecordRequest request)
    {
        return new MedicalRecordInputModel(existing.AppointmentId, existing.PatientId, request.DoctorName, request.Anamnesis, request.PhysicalExam, request.DiagnosticHypothesis, request.Cid10, request.Conduct, request.Notes);
    }
}

public static class MedicalRecordModel
{
    public static MedicalRecordResponse FromDomain(MedicalRecord record)
    {
        return new MedicalRecordResponse(
            record.Id,
            record.AppointmentId,
            record.PatientId,
            record.DoctorName,
            record.Anamnesis,
            record.PhysicalExam,
            record.DiagnosticHypothesis,
            record.Cid10,
            record.Conduct,
            record.Notes,
            record.CreatedAt,
            record.UpdatedAt);
    }
}
