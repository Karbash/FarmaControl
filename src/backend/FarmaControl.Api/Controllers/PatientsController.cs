using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Patients.Models;
using FarmaControl.Application.Care.Patients.UseCases;
using FarmaControl.Contracts.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize(Roles = "admin,gerente,atendimento,atendente,medico,enfermeira,farmaceutico")]
public sealed class PatientsController(
    IUseCase<ListPatientsRequest, IReadOnlyList<PatientResponse>> listUseCase,
    IUseCase<GetPatientRequest, Result<PatientResponse>> getUseCase,
    IUseCase<PatientInputModel, Result<PatientResponse>> createUseCase,
    IUseCase<UpdatePatientCommand, Result<PatientResponse>> updateUseCase,
    IUseCase<DeletePatientCommand, Result<bool>> deleteUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PatientResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PatientResponse> result = await listUseCase.ExecuteAsync(
            new ListPatientsRequest(search, isActive),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PatientResponse>> Get(long id, CancellationToken cancellationToken)
    {
        Result<PatientResponse> result = await getUseCase.ExecuteAsync(
            new GetPatientRequest(id),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<PatientResponse>> Create(
        CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        Result<PatientResponse> result = await createUseCase.ExecuteAsync(
            PatientInputModel.FromRequest(request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<PatientResponse>> Update(
        long id,
        UpdatePatientRequest request,
        CancellationToken cancellationToken)
    {
        Result<PatientResponse> result = await updateUseCase.ExecuteAsync(
            new UpdatePatientCommand(id, PatientInputModel.FromRequest(request)),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,gerente")]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        Result<bool> result = await deleteUseCase.ExecuteAsync(
            new DeletePatientCommand(id),
            cancellationToken);

        return ToEmptyActionResult(result);
    }
}
