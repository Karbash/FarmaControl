using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.MedicalAttendances.Abstractions;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalAttendances.UseCases;

public sealed record GenerateMedicalAttendancePdfRequest(long Id);

public sealed record MedicalAttendancePdfResponse(
    byte[] Content,
    string FileName);

public sealed class GenerateMedicalAttendancePdfUseCase(
    IMedicalAttendanceRepository attendances,
    IMedicalAttendancePdfGenerator pdfGenerator)
    : IUseCase<GenerateMedicalAttendancePdfRequest, Result<MedicalAttendancePdfResponse>>
{
    public async Task<Result<MedicalAttendancePdfResponse>> ExecuteAsync(
        GenerateMedicalAttendancePdfRequest request,
        CancellationToken cancellationToken)
    {
        MedicalAttendance? attendance = await attendances.GetByIdAsync(request.Id, cancellationToken);
        if (attendance is null)
        {
            return Result<MedicalAttendancePdfResponse>.Failure(
                AppError.NotFound("Ficha de atendimento nao encontrada."));
        }

        byte[] content = await pdfGenerator.GenerateAsync(attendance, cancellationToken);
        if (content.Length == 0)
        {
            return Result<MedicalAttendancePdfResponse>.Failure(
                AppError.Validation("PDF nao foi gerado."));
        }

        string patientName = SanitizeFileName(attendance.Name);
        string fileName = $"ficha-atendimento-{attendance.Id}-{patientName}.pdf";

        return Result<MedicalAttendancePdfResponse>.Success(
            new MedicalAttendancePdfResponse(content, fileName));
    }

    private static string SanitizeFileName(string value)
    {
        string sanitized = new(value
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized)
            ? "paciente"
            : sanitized.ToLowerInvariant();
    }
}
