using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.VitalReadings;

public static class VitalRangeCatalog
{
    public static VitalRange GetNormalRange(VitalSignType type) => type switch
    {
        VitalSignType.HeartRate => new VitalRange(60, 100),
        VitalSignType.SpO2 => new VitalRange(95, 100),
        VitalSignType.BodyTemperature => new VitalRange(36.1, 37.2),
        VitalSignType.SystolicBloodPressure => new VitalRange(90, 120),
        VitalSignType.DiastolicBloodPressure => new VitalRange(60, 80),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}