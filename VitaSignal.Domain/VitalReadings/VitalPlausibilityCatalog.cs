using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.VitalReadings;

public static class VitalPlausibilityCatalog
{
    public static VitalRange GetPlausibleRange(VitalSignType type) => type switch
    {
        VitalSignType.HeartRate => new VitalRange(20, 300),
        VitalSignType.SpO2 => new VitalRange(1, 100),
        VitalSignType.BodyTemperature => new VitalRange(25, 45),
        VitalSignType.SystolicBloodPressure => new VitalRange(40, 300),
        VitalSignType.DiastolicBloodPressure => new VitalRange(20, 200),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
