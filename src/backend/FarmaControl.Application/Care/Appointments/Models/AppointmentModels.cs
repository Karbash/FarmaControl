using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Appointments.Models;

public sealed record AppointmentInputModel(
    long PatientId,
    DateOnly? Date,
    TimeOnly? Time,
    string? Type,
    bool IsEmergency,
    string? Responsible,
    string? Notes) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return PatientId <= 0
            ? [AppError.Validation("Paciente e obrigatorio.")]
            : [];
    }

    public CareAppointment ToDomain()
    {
        return CareAppointment.Create(
            PatientId,
            Date ?? DateOnly.FromDateTime(DateTime.Today),
            Time,
            Type,
            IsEmergency,
            Responsible,
            Notes);
    }

    public static AppointmentInputModel FromRequest(CreateAppointmentRequest request)
    {
        return new AppointmentInputModel(
            request.PatientId,
            request.Date,
            request.Time,
            request.Type,
            request.IsEmergency,
            request.Responsible,
            request.Notes);
    }
}

public sealed record UpdateAppointmentModel(string? Type, string? Notes, string? DoctorName);

public sealed record UpdateAppointmentStatusModel(string Status, string? DoctorName) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (string.IsNullOrWhiteSpace(Status))
        {
            errors.Add(AppError.Validation("Status e obrigatorio."));
        }
        else if (!TryParseStatus(Status, out _))
        {
            errors.Add(AppError.Validation("Status invalido."));
        }

        return errors;
    }

    public AppointmentStatus ToDomain() => AppointmentStatus.From(Status);

    public static UpdateAppointmentStatusModel FromRequest(UpdateAppointmentStatusRequest request)
    {
        return new UpdateAppointmentStatusModel(request.Status, request.DoctorName);
    }

    private static bool TryParseStatus(string status, out AppointmentStatus? appointmentStatus)
    {
        try
        {
            appointmentStatus = AppointmentStatus.From(status);
            return true;
        }
        catch
        {
            appointmentStatus = null;
            return false;
        }
    }
}

public static class AppointmentModel
{
    public static AppointmentResponse FromDomain(CareAppointment appointment)
    {
        return new AppointmentResponse(
            appointment.Id,
            appointment.PatientId,
            appointment.Date,
            appointment.Time,
            appointment.Type,
            appointment.IsEmergency,
            appointment.Status.Value,
            appointment.DoctorName,
            appointment.Responsible,
            appointment.Notes,
            appointment.CreatedAt,
            appointment.UpdatedAt);
    }
}
