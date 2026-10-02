using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.VitalReadings;

public sealed class VitalReading
{
    public const int DeviceIdMaxLength = 100;

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
        DateTime recordedAt, string deviceId)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient id is required.", nameof(patientId));

        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type), $"Unknown vital sign type '{type}'.");

        var plausibleRange = VitalPlausibilityCatalog.GetPlausibleRange(type);
        if (!plausibleRange.Contains(value))
            throw new ArgumentOutOfRangeException(nameof(value),
                $"{type} value {value} is outside what a device can measure ({plausibleRange.Min}-{plausibleRange.Max}).");

        var recordedAtUtc = ToUtc(recordedAt);

        if (recordedAtUtc > DateTime.UtcNow.AddMinutes(1))
            throw new ArgumentException("Recorded time cannot be in the future.", nameof(recordedAt));

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device id is required.", nameof(deviceId));

        var trimmedDeviceId = deviceId.Trim();
        if (trimmedDeviceId.Length > DeviceIdMaxLength)
            throw new ArgumentException($"Device id cannot exceed {DeviceIdMaxLength} characters.", nameof(deviceId));

        return new VitalReading(Guid.NewGuid(), patientId, type, value, UnitFor(type), recordedAtUtc, trimmedDeviceId);
    }

    private static DateTime ToUtc(DateTime recordedAt)
    {
        if (recordedAt == default)
            throw new ArgumentException("Recorded time is required.", nameof(recordedAt));

        if (recordedAt.Kind == DateTimeKind.Unspecified)
            throw new ArgumentException(
                "Recorded time must include a timezone (e.g. '2026-10-01T10:00:00Z' or '2026-10-01T07:00:00-03:00').",
                nameof(recordedAt));

        return recordedAt.ToUniversalTime();
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