using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Appointments.Abstractions;
using FarmaControl.Application.Care.MedicalRecords.Abstractions;
using FarmaControl.Application.Care.MedicalRecords.Models;
using FarmaControl.Application.Care.Patients.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalRecords.UseCases;

public sealed record ListMedicalRecordsRequest(long? AppointmentId, long? PatientId);

public sealed class ListMedicalRecordsUseCase(IMedicalRecordRepository records)
    : IUseCase<ListMedicalRecordsRequest, IReadOnlyList<MedicalRecordResponse>>
{
    public async Task<IReadOnlyList<MedicalRecordResponse>> ExecuteAsync(
        ListMedicalRecordsRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MedicalRecord> result = await records.ListAsync(request.AppointmentId, request.PatientId, cancellationToken);
        return result.Select(MedicalRecordModel.FromDomain).ToArray();
    }
}

public sealed record GetMedicalRecordRequest(long Id);

public sealed class GetMedicalRecordUseCase(IMedicalRecordRepository records)
    : IUseCase<GetMedicalRecordRequest, Result<MedicalRecordResponse>>
{
    public async Task<Result<MedicalRecordResponse>> ExecuteAsync(
        GetMedicalRecordRequest request,
        CancellationToken cancellationToken)
    {
        MedicalRecord? record = await records.GetByIdAsync(request.Id, cancellationToken);
        return record is null
            ? Result<MedicalRecordResponse>.Failure(AppError.NotFound("Prontuario nao encontrado."))
            : Result<MedicalRecordResponse>.Success(MedicalRecordModel.FromDomain(record));
    }
}

public sealed class CreateMedicalRecordUseCase(
    IMedicalRecordRepository records,
    IAppointmentRepository appointments,
    IPatientRepository patients,
    IUnitOfWork unitOfWork)
    : IUseCase<MedicalRecordInputModel, Result<MedicalRecordResponse>>
{
    public async Task<Result<MedicalRecordResponse>> ExecuteAsync(
        MedicalRecordInputModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<MedicalRecordResponse>.Failure(errors.FirstOrDefaultError());
        }

        CareAppointment? appointment = await appointments.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result<MedicalRecordResponse>.Failure(AppError.NotFound("Atendimento nao encontrado."));
        }

        Patient? patient = await patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result<MedicalRecordResponse>.Failure(AppError.NotFound("Paciente nao encontrado."));
        }

        MedicalRecord record = request.ToDomain();

        await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                appointment.ChangeStatus(AppointmentStatus.InCare, request.DoctorName);
                await records.AddAsync(record, ct);
            },
            cancellationToken);

        return Result<MedicalRecordResponse>.Success(MedicalRecordModel.FromDomain(record));
    }
}

public sealed record UpdateMedicalRecordCommand(long Id, UpdateMedicalRecordRequest Request);

public sealed class UpdateMedicalRecordUseCase(IMedicalRecordRepository records)
    : IUseCase<UpdateMedicalRecordCommand, Result<MedicalRecordResponse>>
{
    public async Task<Result<MedicalRecordResponse>> ExecuteAsync(
        UpdateMedicalRecordCommand request,
        CancellationToken cancellationToken)
    {
        MedicalRecord? record = await records.GetByIdAsync(request.Id, cancellationToken);
        if (record is null)
        {
            return Result<MedicalRecordResponse>.Failure(AppError.NotFound("Prontuario nao encontrado."));
        }

        MedicalRecordInputModel model = MedicalRecordInputModel.FromRequest(record, request.Request);
        IReadOnlyList<AppError> errors = model.Validate();
        if (errors.HasErrors())
        {
            return Result<MedicalRecordResponse>.Failure(errors.FirstOrDefaultError());
        }

        model.ApplyTo(record);
        await records.SaveChangesAsync(cancellationToken);

        return Result<MedicalRecordResponse>.Success(MedicalRecordModel.FromDomain(record));
    }
}
