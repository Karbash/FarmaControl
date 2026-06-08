using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalAttendances.Models;

public sealed record VitalSignsModel(
    int? SystolicPressure,
    int? DiastolicPressure,
    decimal? Temperature,
    decimal? BloodGlucose,
    int? OxygenSaturation,
    int? HeartRate) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (SystolicPressure is <= 0)
        {
            errors.Add(AppError.Validation("Pressao sistolica deve ser maior que zero."));
        }

        if (DiastolicPressure is <= 0)
        {
            errors.Add(AppError.Validation("Pressao diastolica deve ser maior que zero."));
        }

        if (OxygenSaturation is < 0 or > 100)
        {
            errors.Add(AppError.Validation("Saturacao deve estar entre 0 e 100."));
        }

        if (HeartRate is <= 0)
        {
            errors.Add(AppError.Validation("Frequencia cardiaca deve ser maior que zero."));
        }

        return errors;
    }

    public VitalSigns ToDomain()
    {
        return VitalSigns.Create(
            SystolicPressure,
            DiastolicPressure,
            Temperature,
            BloodGlucose,
            OxygenSaturation,
            HeartRate);
    }

    public static VitalSignsModel Empty()
    {
        return new VitalSignsModel(null, null, null, null, null, null);
    }

    public static VitalSignsModel FromRequest(VitalSignsRequest? request)
    {
        return request is null
            ? Empty()
            : new VitalSignsModel(
                request.SystolicPressure,
                request.DiastolicPressure,
                request.Temperature,
                request.BloodGlucose,
                request.OxygenSaturation,
                request.HeartRate);
    }

    public static VitalSignsResponse FromDomain(VitalSigns vitalSigns)
    {
        return new VitalSignsResponse(
            vitalSigns.SystolicPressure,
            vitalSigns.DiastolicPressure,
            vitalSigns.Temperature,
            vitalSigns.BloodGlucose,
            vitalSigns.OxygenSaturation,
            vitalSigns.HeartRate);
    }
}
