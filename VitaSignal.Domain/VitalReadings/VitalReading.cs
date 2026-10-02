using VitaSignal.Domain.Common;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.VitalReadings;

public sealed class VitalReading
{
    public const int DeviceIdMaxLength = 100;

    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(1);

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
        DateTime recordedAt, string deviceId, DateTimeOffset now)
    {
        if (patientId == Guid.Empty)
            throw new DomainValidationException("Patient id is required.");

        if (!Enum.IsDefined(type))
            throw new DomainValidationException($"Unknown vital sign type '{type}'.");

        var plausibleRange = VitalPlausibilityCatalog.GetPlausibleRange(type);
        if (!plausibleRange.Contains(value))
            throw new ImplausibleVitalValueException(type, value, plausibleRange);

        var recordedAtUtc = ToUtc(recordedAt);

        if (recordedAtUtc > now.UtcDateTime.Add(ClockSkewTolerance))
            throw new DomainValidationException("Recorded time cannot be in the future.");

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new DomainValidationException("Device id is required.");

        var trimmedDeviceId = deviceId.Trim();
        if (trimmedDeviceId.Length > DeviceIdMaxLength)
            throw new DomainValidationException($"Device id cannot exceed {DeviceIdMaxLength} characters.");

        return new VitalReading(Guid.NewGuid(), patientId, type, value, UnitFor(type), recordedAtUtc, trimmedDeviceId);
    }

    private static DateTime ToUtc(DateTime recordedAt)
    {
        if (recordedAt == default)
            throw new DomainValidationException("Recorded time is required.");

        if (recordedAt.Kind == DateTimeKind.Unspecified)
            throw new DomainValidationException(
                "Recorded time must include a timezone (e.g. '2026-10-01T10:00:00Z' or '2026-10-01T07:00:00-03:00').");

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
