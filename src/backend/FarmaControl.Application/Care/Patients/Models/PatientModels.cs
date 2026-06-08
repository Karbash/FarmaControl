using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Patients.Models;

public sealed record PatientInputModel(
    string Name,
    string? Cpf,
    DateOnly? BirthDate,
    string? Sex,
    string? Phone,
    string? Address,
    string? Notes,
    IReadOnlyList<string> Comorbidities,
    bool IsActive) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? [AppError.Validation("Nome e obrigatorio.")]
            : [];
    }

    public Patient ToDomain()
    {
        return Patient.Create(Name, Cpf, BirthDate, Sex, Phone, Address, Notes, Comorbidities);
    }

    public void ApplyTo(Patient patient)
    {
        patient.Update(Name, Cpf, BirthDate, Sex, Phone, Address, Notes, Comorbidities, IsActive);
    }

    public static PatientInputModel FromRequest(CreatePatientRequest request)
    {
        return new PatientInputModel(
            request.Name,
            request.Cpf,
            request.BirthDate,
            request.Sex,
            request.Phone,
            request.Address,
            request.Notes,
            request.Comorbidities ?? [],
            true);
    }

    public static PatientInputModel FromRequest(UpdatePatientRequest request)
    {
        return new PatientInputModel(
            request.Name,
            request.Cpf,
            request.BirthDate,
            request.Sex,
            request.Phone,
            request.Address,
            request.Notes,
            request.Comorbidities ?? [],
            request.IsActive);
    }
}

public static class PatientModel
{
    public static PatientResponse FromDomain(Patient patient)
    {
        return new PatientResponse(
            patient.Id,
            patient.Name,
            patient.Cpf,
            patient.BirthDate,
            patient.Sex,
            patient.Phone,
            patient.Address,
            patient.Notes,
            SplitComorbidities(patient.Comorbidities),
            patient.IsActive,
            patient.CreatedAt,
            patient.UpdatedAt);
    }

    private static IReadOnlyList<string> SplitComorbidities(string? comorbidities)
    {
        return string.IsNullOrWhiteSpace(comorbidities)
            ? []
            : comorbidities
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}
