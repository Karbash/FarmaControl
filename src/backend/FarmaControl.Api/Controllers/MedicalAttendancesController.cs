using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.MedicalAttendances.UseCases;
using FarmaControl.Contracts.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin,gerente,medico,enfermagem,enfermeiro,enfermeira,farmaceutico,movimentacao,saida")]
public sealed class MedicalAttendancesController(
    IUseCase<GetMedicalAttendanceRequest, Result<MedicalAttendanceResponse>> getUseCase,
    IUseCase<GetMedicalAttendanceByAppointmentRequest, Result<MedicalAttendanceResponse>> getByAppointmentUseCase,
    IUseCase<CreateMedicalAttendanceCommand, Result<MedicalAttendanceResponse>> createUseCase,
    IUseCase<UpdateMedicalAttendanceCommand, Result<MedicalAttendanceResponse>> updateUseCase,
    IUseCase<GenerateMedicalAttendancePdfRequest, Result<MedicalAttendancePdfResponse>> pdfUseCase)
    : ApiControllerBase
{
    [HttpGet("api/medical-attendances/{id:long}")]
    public async Task<ActionResult<MedicalAttendanceResponse>> Get(
        long id,
        CancellationToken cancellationToken)
    {
        Result<MedicalAttendanceResponse> result = await getUseCase.ExecuteAsync(
            new GetMedicalAttendanceRequest(id),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("api/appointments/{appointmentId:long}/medical-attendance")]
    public async Task<ActionResult<MedicalAttendanceResponse>> GetByAppointment(
        long appointmentId,
        CancellationToken cancellationToken)
    {
        Result<MedicalAttendanceResponse> result = await getByAppointmentUseCase.ExecuteAsync(
            new GetMedicalAttendanceByAppointmentRequest(appointmentId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("api/appointments/{appointmentId:long}/medical-attendance")]
    public async Task<ActionResult<MedicalAttendanceResponse>> Create(
        long appointmentId,
        CreateMedicalAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        Result<MedicalAttendanceResponse> result = await createUseCase.ExecuteAsync(
            new CreateMedicalAttendanceCommand(appointmentId, CurrentUserId(), request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("api/medical-attendances/{id:long}")]
    public async Task<ActionResult<MedicalAttendanceResponse>> Update(
        long id,
        UpdateMedicalAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        Result<MedicalAttendanceResponse> result = await updateUseCase.ExecuteAsync(
            new UpdateMedicalAttendanceCommand(id, CurrentUserId(), request),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("api/medical-attendances/{id:long}/pdf")]
    public async Task<IActionResult> Pdf(long id, CancellationToken cancellationToken)
    {
        Result<MedicalAttendancePdfResponse> result = await pdfUseCase.ExecuteAsync(
            new GenerateMedicalAttendancePdfRequest(id),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            AppError error = result.Error ?? AppError.Validation("Erro desconhecido.");

            return error.Code switch
            {
                "not_found" => NotFound(new { error = error.Message }),
                "forbidden" => Forbid(),
                "conflict" => Conflict(new { error = error.Message }),
                _ => BadRequest(new { error = error.Message })
            };
        }

        return File(result.Value.Content, "application/pdf", result.Value.FileName);
    }
}
