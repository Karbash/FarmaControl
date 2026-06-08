using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Patients.Abstractions;
using FarmaControl.Application.Care.Patients.Models;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Patients.UseCases;

public sealed record ListPatientsRequest(string? Search, bool? IsActive);

public sealed class ListPatientsUseCase(IPatientRepository patients)
    : IUseCase<ListPatientsRequest, IReadOnlyList<PatientResponse>>
{
    public async Task<IReadOnlyList<PatientResponse>> ExecuteAsync(
        ListPatientsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await patients.ListAsync(request.Search, request.IsActive, cancellationToken);
        return result.Select(PatientModel.FromDomain).ToArray();
    }
}

public sealed record GetPatientRequest(long Id);

public sealed class GetPatientUseCase(IPatientRepository patients)
    : IUseCase<GetPatientRequest, Result<PatientResponse>>
{
    public async Task<Result<PatientResponse>> ExecuteAsync(GetPatientRequest request, CancellationToken cancellationToken)
    {
        Patient? patient = await patients.GetByIdAsync(request.Id, cancellationToken);
        return patient is null
            ? Result<PatientResponse>.Failure(AppError.NotFound("Paciente nao encontrado."))
            : Result<PatientResponse>.Success(PatientModel.FromDomain(patient));
    }
}

public sealed class CreatePatientUseCase(IPatientRepository patients)
    : IUseCase<PatientInputModel, Result<PatientResponse>>
{
    public async Task<Result<PatientResponse>> ExecuteAsync(PatientInputModel request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<PatientResponse>.Failure(errors.FirstOrDefaultError());
        }

        Patient patient = request.ToDomain();
        await patients.AddAsync(patient, cancellationToken);
        await patients.SaveChangesAsync(cancellationToken);

        return Result<PatientResponse>.Success(PatientModel.FromDomain(patient));
    }
}

public sealed record UpdatePatientCommand(long Id, PatientInputModel Model);

public sealed class UpdatePatientUseCase(IPatientRepository patients)
    : IUseCase<UpdatePatientCommand, Result<PatientResponse>>
{
    public async Task<Result<PatientResponse>> ExecuteAsync(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Model.Validate();
        if (errors.HasErrors())
        {
            return Result<PatientResponse>.Failure(errors.FirstOrDefaultError());
        }

        Patient? patient = await patients.GetByIdAsync(request.Id, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(AppError.NotFound("Paciente nao encontrado."));
        }

        request.Model.ApplyTo(patient);
        await patients.SaveChangesAsync(cancellationToken);

        return Result<PatientResponse>.Success(PatientModel.FromDomain(patient));
    }
}

public sealed record DeletePatientCommand(long Id);

public sealed class DeletePatientUseCase(IPatientRepository patients)
    : IUseCase<DeletePatientCommand, Result<bool>>
{
    public async Task<Result<bool>> ExecuteAsync(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        Patient? patient = await patients.GetByIdAsync(request.Id, cancellationToken);
        if (patient is null)
        {
            return Result<bool>.Failure(AppError.NotFound("Paciente nao encontrado."));
        }

        patients.Remove(patient);
        await patients.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
