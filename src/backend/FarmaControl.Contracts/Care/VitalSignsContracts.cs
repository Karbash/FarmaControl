namespace FarmaControl.Contracts.Care;

public sealed record VitalSignsRequest(
    int? SystolicPressure,
    int? DiastolicPressure,
    decimal? Temperature,
    decimal? BloodGlucose,
    int? OxygenSaturation,
    int? HeartRate);

public sealed record VitalSignsResponse(
    int? SystolicPressure,
    int? DiastolicPressure,
    decimal? Temperature,
    decimal? BloodGlucose,
    int? OxygenSaturation,
    int? HeartRate);
