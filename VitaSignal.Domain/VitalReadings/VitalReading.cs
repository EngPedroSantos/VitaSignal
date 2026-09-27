using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.VitalReadings;

public sealed class VitalReading
{
    public Guid Id { get; }
    public Guid PatientId { get; }
    public VitalSignType Type { get; }
    public double Value { get; }
    public string Unit { get; }
    public DateTime RecordedAtUtc { get; }
    public string DeviceId { get; }

    private VitalReading(
        Guid id, Guid patientId, VitalSignType type,
        double value, string unit, DateTime recordedAtUtc, string deviceId)
    {
        Id = id;
        PatientId = patientId;
        Type = type;
        Value = value;
        Unit = unit;
        RecordedAtUtc = recordedAtUtc;
        DeviceId = deviceId;
    }

    public static VitalReading Create(
        Guid patientId, VitalSignType type, double value,
        DateTime recordedAtUtc, string deviceId)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient id is required.", nameof(patientId));

        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Vital sign value must be positive.");

        if (recordedAtUtc > DateTime.UtcNow.AddMinutes(1))
            throw new ArgumentException("Recorded time cannot be in the future.", nameof(recordedAtUtc));

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device id is required.", nameof(deviceId));

        return new VitalReading(Guid.NewGuid(), patientId, type, value, UnitFor(type), recordedAtUtc, deviceId.Trim());
    }

    private static string UnitFor(VitalSignType type) => type switch
    {
        VitalSignType.HeartRate => "bpm",
        VitalSignType.SpO2 => "%",
        VitalSignType.BodyTemperature => "°C",
        VitalSignType.SystolicBloodPressure => "mmHg",
        VitalSignType.DiastolicBloodPressure => "mmHg",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}