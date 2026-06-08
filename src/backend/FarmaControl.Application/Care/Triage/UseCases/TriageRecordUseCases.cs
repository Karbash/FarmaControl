using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Appointments.Abstractions;
using FarmaControl.Application.Care.Triage.Abstractions;
using FarmaControl.Application.Care.Triage.Models;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Triage.UseCases;

public sealed record ListTriageRecordsRequest(long AppointmentId);

public sealed class ListTriageRecordsUseCase(ITriageRecordRepository triageRecords)
    : IUseCase<ListTriageRecordsRequest, Result<IReadOnlyList<TriageRecordResponse>>>
{
    public async Task<Result<IReadOnlyList<TriageRecordResponse>>> ExecuteAsync(
        ListTriageRecordsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
        {
            return Result<IReadOnlyList<TriageRecordResponse>>.Failure(AppError.Validation("Atendimento e obrigatorio."));
        }

        IReadOnlyList<TriageRecord> result = await triageRecords.ListByAppointmentAsync(request.AppointmentId, cancellationToken);
        return Result<IReadOnlyList<TriageRecordResponse>>.Success(result.Select(TriageRecordModel.FromDomain).ToArray());
    }
}

public sealed class CreateTriageRecordUseCase(
    ITriageRecordRepository triageRecords,
    IAppointmentRepository appointments,
    IUnitOfWork unitOfWork)
    : IUseCase<TriageRecordInputModel, Result<TriageRecordResponse>>
{
    public async Task<Result<TriageRecordResponse>> ExecuteAsync(
        TriageRecordInputModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<TriageRecordResponse>.Failure(errors.FirstOrDefaultError());
        }

        CareAppointment? appointment = await appointments.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result<TriageRecordResponse>.Failure(AppError.NotFound("Atendimento nao encontrado."));
        }

        TriageRecord triage = request.ToDomain();

        await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                if (appointment.Status == AppointmentStatus.Waiting)
                {
                    appointment.ChangeStatus(AppointmentStatus.Triage, null);
                }

                await triageRecords.AddAsync(triage, ct);
            },
            cancellationToken);

        return Result<TriageRecordResponse>.Success(TriageRecordModel.FromDomain(triage));
    }
}

public sealed record UpdateTriageRecordCommand(long Id, TriageRecordInputModel Model);

public sealed class UpdateTriageRecordUseCase(ITriageRecordRepository triageRecords)
    : IUseCase<UpdateTriageRecordCommand, Result<TriageRecordResponse>>
{
    public async Task<Result<TriageRecordResponse>> ExecuteAsync(
        UpdateTriageRecordCommand request,
        CancellationToken cancellationToken)
    {
        TriageRecord? triage = await triageRecords.GetByIdAsync(request.Id, cancellationToken);
        if (triage is null)
        {
            return Result<TriageRecordResponse>.Failure(AppError.NotFound("Triagem nao encontrada."));
        }

        TriageRecordInputModel model = request.Model with { AppointmentId = triage.AppointmentId };
        IReadOnlyList<AppError> errors = model.Validate();
        if (errors.HasErrors())
        {
            return Result<TriageRecordResponse>.Failure(errors.FirstOrDefaultError());
        }

        model.ApplyTo(triage);
        await triageRecords.SaveChangesAsync(cancellationToken);

        return Result<TriageRecordResponse>.Success(TriageRecordModel.FromDomain(triage));
    }
}
