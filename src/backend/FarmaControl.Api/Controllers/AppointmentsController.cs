using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Appointments.Models;
using FarmaControl.Application.Care.Appointments.UseCases;
using FarmaControl.Contracts.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize(Roles = "admin,gerente,atendimento,atendente,medico,enfermeira,farmaceutico,movimentacao,saida")]
public sealed class AppointmentsController(
    IUseCase<ListAppointmentsRequest, IReadOnlyList<AppointmentResponse>> listUseCase,
    IUseCase<GetAppointmentRequest, Result<AppointmentResponse>> getUseCase,
    IUseCase<AppointmentInputModel, Result<AppointmentResponse>> createUseCase,
    IUseCase<UpdateAppointmentCommand, Result<AppointmentResponse>> updateUseCase,
    IUseCase<UpdateAppointmentStatusCommand, Result<AppointmentResponse>> updateStatusUseCase,
    IUseCase<DeleteAppointmentCommand, Result<bool>> deleteUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> List(
        [FromQuery] DateOnly? date,
        [FromQuery] string? status,
        [FromQuery] long? patientId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppointmentResponse> result = await listUseCase.ExecuteAsync(
            new ListAppointmentsRequest(date, status, patientId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AppointmentResponse>> Get(long id, CancellationToken cancellationToken)
    {
        Result<AppointmentResponse> result = await getUseCase.ExecuteAsync(
            new GetAppointmentRequest(id),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentResponse>> Create(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        Result<AppointmentResponse> result = await createUseCase.ExecuteAsync(
            AppointmentInputModel.FromRequest(request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin,gerente,medico,enfermagem,enfermeiro,enfermeira,farmaceutico")]
    public async Task<ActionResult<AppointmentResponse>> Update(
        long id,
        UpdateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        Result<AppointmentResponse> result = await updateUseCase.ExecuteAsync(
            new UpdateAppointmentCommand(
                id,
                new UpdateAppointmentModel(request.Type, request.Notes, request.DoctorName)),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "admin,gerente,medico,enfermagem,enfermeiro,enfermeira,farmaceutico")]
    public async Task<ActionResult<AppointmentResponse>> UpdateStatus(
        long id,
        UpdateAppointmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        Result<AppointmentResponse> result = await updateStatusUseCase.ExecuteAsync(
            new UpdateAppointmentStatusCommand(id, UpdateAppointmentStatusModel.FromRequest(request)),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,gerente")]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        Result<bool> result = await deleteUseCase.ExecuteAsync(
            new DeleteAppointmentCommand(id),
            cancellationToken);

        return ToEmptyActionResult(result);
    }
}
