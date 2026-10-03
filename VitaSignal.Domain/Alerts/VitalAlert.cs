using VitaSignal.Domain.Alerts.Enums;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.Alerts;

public sealed class VitalAlert
{
    public Guid Id { get; }
    public Guid PatientId { get; }
    public Guid ReadingId { get; }
    public VitalSignType Type { get; }
    public double Value { get; }
    public AlertSeverity Severity { get; }
    public DateTime RaisedAtUtc { get; }
    public DateTime? AcknowledgedAtUtc { get; private set; }

    public bool IsAcknowledged => AcknowledgedAtUtc is not null;

    private VitalAlert(
        Guid id, Guid patientId, Guid readingId, VitalSignType type, double value,
        AlertSeverity severity, DateTime raisedAtUtc, DateTime? acknowledgedAtUtc)
    {
        Id = id;
        PatientId = patientId;
        ReadingId = readingId;
        Type = type;
        Value = value;
        Severity = severity;
        RaisedAtUtc = raisedAtUtc;
        AcknowledgedAtUtc = acknowledgedAtUtc;
    }

    public static AlertSeverity? Evaluate(VitalSignType type, double value)
    {
        if (!VitalRangeCatalog.GetCriticalRange(type).Contains(value))
            return AlertSeverity.Critical;

        if (!VitalRangeCatalog.GetNormalRange(type).Contains(value))
            return AlertSeverity.Warning;

        return null;
    }

    public static VitalAlert? RaiseIfNeeded(VitalReading reading, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var severity = Evaluate(reading.Type, reading.Value);
        if (severity is null)
            return null;

        return new VitalAlert(
            Guid.NewGuid(), reading.PatientId, reading.Id, reading.Type, reading.Value,
            severity.Value, now.UtcDateTime, acknowledgedAtUtc: null);
    }

    public void Acknowledge(DateTimeOffset now)
    {
        if (IsAcknowledged)
            return;

        AcknowledgedAtUtc = now.UtcDateTime;
    }
}
