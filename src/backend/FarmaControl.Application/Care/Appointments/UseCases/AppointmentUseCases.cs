using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Appointments.Abstractions;
using FarmaControl.Application.Care.Appointments.Models;
using FarmaControl.Application.Care.MedicalAttendances.Abstractions;
using FarmaControl.Application.Care.Patients.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Appointments.UseCases;

public sealed record ListAppointmentsRequest(DateOnly? Date, string? Status, long? PatientId);

public sealed class ListAppointmentsUseCase(IAppointmentRepository appointments)
    : IUseCase<ListAppointmentsRequest, IReadOnlyList<AppointmentResponse>>
{
    public async Task<IReadOnlyList<AppointmentResponse>> ExecuteAsync(
        ListAppointmentsRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CareAppointment> result = await appointments.ListAsync(
            request.Date,
            request.Status,
            request.PatientId,
            cancellationToken);

        return result.Select(AppointmentModel.FromDomain).ToArray();
    }
}

public sealed record GetAppointmentRequest(long Id);

public sealed class GetAppointmentUseCase(IAppointmentRepository appointments)
    : IUseCase<GetAppointmentRequest, Result<AppointmentResponse>>
{
    public async Task<Result<AppointmentResponse>> ExecuteAsync(
        GetAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        CareAppointment? appointment = await appointments.GetByIdAsync(request.Id, cancellationToken);
        return appointment is null
            ? Result<AppointmentResponse>.Failure(AppError.NotFound("Atendimento nao encontrado."))
            : Result<AppointmentResponse>.Success(AppointmentModel.FromDomain(appointment));
    }
}

public sealed class CreateAppointmentUseCase(
    IAppointmentRepository appointments,
    IPatientRepository patients)
    : IUseCase<AppointmentInputModel, Result<AppointmentResponse>>
{
    public async Task<Result<AppointmentResponse>> ExecuteAsync(
        AppointmentInputModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<AppointmentResponse>.Failure(errors.FirstOrDefaultError());
        }

        Patient? patient = await patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result<AppointmentResponse>.Failure(AppError.NotFound("Paciente nao encontrado."));
        }

        CareAppointment appointment = request.ToDomain();
        await appointments.AddAsync(appointment, cancellationToken);
        await appointments.SaveChangesAsync(cancellationToken);

        return Result<AppointmentResponse>.Success(AppointmentModel.FromDomain(appointment));
    }
}

public sealed record UpdateAppointmentCommand(long Id, UpdateAppointmentModel Model);

public sealed class UpdateAppointmentUseCase(IAppointmentRepository appointments)
    : IUseCase<UpdateAppointmentCommand, Result<AppointmentResponse>>
{
    public async Task<Result<AppointmentResponse>> ExecuteAsync(
        UpdateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        CareAppointment? appointment = await appointments.GetByIdAsync(request.Id, cancellationToken);
        if (appointment is null)
        {
            return Result<AppointmentResponse>.Failure(AppError.NotFound("Atendimento nao encontrado."));
        }

        appointment.Update(request.Model.Type, request.Model.Notes, request.Model.DoctorName);
        await appointments.SaveChangesAsync(cancellationToken);

        return Result<AppointmentResponse>.Success(AppointmentModel.FromDomain(appointment));
    }
}

public sealed record UpdateAppointmentStatusCommand(long Id, UpdateAppointmentStatusModel Model);

public sealed class UpdateAppointmentStatusUseCase(
    IAppointmentRepository appointments,
    IMedicalAttendanceRepository medicalAttendances)
    : IUseCase<UpdateAppointmentStatusCommand, Result<AppointmentResponse>>
{
    public async Task<Result<AppointmentResponse>> ExecuteAsync(
        UpdateAppointmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Model.Validate();
        if (errors.HasErrors())
        {
            return Result<AppointmentResponse>.Failure(errors.FirstOrDefaultError());
        }

        CareAppointment? appointment = await appointments.GetByIdAsync(request.Id, cancellationToken);
        if (appointment is null)
        {
            return Result<AppointmentResponse>.Failure(AppError.NotFound("Atendimento nao encontrado."));
        }

        AppointmentStatus nextStatus = request.Model.ToDomain();
        if (nextStatus == AppointmentStatus.Closed)
        {
            MedicalAttendance? attendance = await medicalAttendances.GetByAppointmentIdAsync(
                appointment.Id,
                cancellationToken);

            if (HasPendingDispensation(attendance))
            {
                return Result<AppointmentResponse>.Failure(AppError.Validation(
                    "Nao e possivel encerrar o atendimento enquanto houver medicamento prescrito aguardando dispensacao."));
            }
        }

        appointment.ChangeStatus(nextStatus, request.Model.DoctorName);
        await appointments.SaveChangesAsync(cancellationToken);

        return Result<AppointmentResponse>.Success(AppointmentModel.FromDomain(appointment));
    }

    private static bool HasPendingDispensation(MedicalAttendance? attendance)
    {
        if (attendance is null)
        {
            return false;
        }

        return attendance.Prescriptions
            .Where(prescription => prescription.Id > 0)
            .Any(prescription => !attendance.Dispensations.Any(dispensation =>
                dispensation.PrescriptionId == prescription.Id));
    }
}

public sealed record DeleteAppointmentCommand(long Id);

public sealed class DeleteAppointmentUseCase(IAppointmentRepository appointments)
    : IUseCase<DeleteAppointmentCommand, Result<bool>>
{
    public async Task<Result<bool>> ExecuteAsync(DeleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        CareAppointment? appointment = await appointments.GetByIdAsync(request.Id, cancellationToken);
        if (appointment is null)
        {
            return Result<bool>.Failure(AppError.NotFound("Atendimento nao encontrado."));
        }

        appointments.Remove(appointment);
        await appointments.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
