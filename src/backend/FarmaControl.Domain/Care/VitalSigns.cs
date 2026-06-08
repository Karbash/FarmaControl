namespace FarmaControl.Domain.Care;

public sealed class VitalSigns
{
    public int? SystolicPressure { get; private set; }

    public int? DiastolicPressure { get; private set; }

    public decimal? Temperature { get; private set; }

    public decimal? BloodGlucose { get; private set; }

    public int? OxygenSaturation { get; private set; }

    public int? HeartRate { get; private set; }

    public static VitalSigns Create(
        int? systolicPressure,
        int? diastolicPressure,
        decimal? temperature,
        decimal? bloodGlucose,
        int? oxygenSaturation,
        int? heartRate)
    {
        return new VitalSigns
        {
            SystolicPressure = systolicPressure,
            DiastolicPressure = diastolicPressure,
            Temperature = temperature,
            BloodGlucose = bloodGlucose,
            OxygenSaturation = oxygenSaturation,
            HeartRate = heartRate
        };
    }
}
