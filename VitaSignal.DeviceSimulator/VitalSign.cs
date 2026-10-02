namespace VitaSignal.DeviceSimulator;

internal enum VitalSign
{
    HeartRate,
    SpO2,
    BodyTemperature,
    SystolicBloodPressure,
    DiastolicBloodPressure
}

internal enum Scenario
{
    Stable,
    Tachycardia,
    Fever,
    Desaturation,
    Hypertension
}

internal readonly record struct SimulatedReading(VitalSign Type, double Value);
