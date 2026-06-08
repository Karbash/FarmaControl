using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Triage.Models;

public sealed record TriageRecordInputModel(
    long AppointmentId,
    string? BloodPressure,
    string? Temperature,
    string? Weight,
    string? Height,
    string? HeartRate,
    string? OxygenSaturation,
    string? ChiefComplaint,
    string? Responsible,
    string? Notes) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return AppointmentId <= 0
            ? [AppError.Validation("Atendimento e obrigatorio.")]
            : [];
    }

    public TriageRecord ToDomain()
    {
        return TriageRecord.Create(AppointmentId, BloodPressure, Temperature, Weight, Height, HeartRate, OxygenSaturation, ChiefComplaint, Responsible, Notes);
    }

    public void ApplyTo(TriageRecord triage)
    {
        triage.Update(BloodPressure, Temperature, Weight, Height, HeartRate, OxygenSaturation, ChiefComplaint, Responsible, Notes);
    }

    public static TriageRecordInputModel FromRequest(CreateTriageRecordRequest request)
    {
        return new TriageRecordInputModel(request.AppointmentId, request.BloodPressure, request.Temperature, request.Weight, request.Height, request.HeartRate, request.OxygenSaturation, request.ChiefComplaint, request.Responsible, request.Notes);
    }

    public static TriageRecordInputModel FromRequest(long appointmentId, UpdateTriageRecordRequest request)
    {
        return new TriageRecordInputModel(appointmentId, request.BloodPressure, request.Temperature, request.Weight, request.Height, request.HeartRate, request.OxygenSaturation, request.ChiefComplaint, request.Responsible, request.Notes);
    }
}

public static class TriageRecordModel
{
    public static TriageRecordResponse FromDomain(TriageRecord triage)
    {
        return new TriageRecordResponse(
            triage.Id,
            triage.AppointmentId,
            triage.BloodPressure,
            triage.Temperature,
            triage.Weight,
            triage.Height,
            triage.HeartRate,
            triage.OxygenSaturation,
            triage.ChiefComplaint,
            triage.Responsible,
            triage.Notes,
            triage.CreatedAt,
            triage.UpdatedAt);
    }
}
