using System.Diagnostics.Metrics;
using VitaSignal.Domain.Alerts.Enums;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Application.VitalReadings;

public sealed class VitalReadingMetrics
{
    public const string MeterName = "VitaSignal.Application";

    private readonly Counter<long> _readingsRegistered;
    private readonly Counter<long> _readingsOutOfRange;
    private readonly Counter<long> _readingsRejected;
    private readonly Counter<long> _alertsRaised;
    private readonly Histogram<double> _registrationDuration;

    public VitalReadingMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName);

        _readingsRegistered = meter.CreateCounter<long>(
            "vitasignal.vital_readings.registered",
            description: "Total vital sign readings recorded, by type.");

        _readingsOutOfRange = meter.CreateCounter<long>(
            "vitasignal.vital_readings.out_of_range",
            description: "Total readings recorded outside the normal range, by type.");

        _readingsRejected = meter.CreateCounter<long>(
            "vitasignal.vital_readings.rejected",
            description: "Total readings rejected before being stored, by reason.");

        _alertsRaised = meter.CreateCounter<long>(
            "vitasignal.alerts.raised",
            description: "Total clinical alerts raised, by severity and type.");

        _registrationDuration = meter.CreateHistogram(
            "vitasignal.vital_readings.registration_duration",
            unit: "s",
            description: "Duration of vital sign reading record processing.",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5]
            });
    }

    public void RecordRegistered(VitalSignType type, bool isWithinNormalRange, TimeSpan elapsed)
    {
        var typeTag = TypeTag(type);

        _readingsRegistered.Add(1, typeTag);
        if (!isWithinNormalRange)
            _readingsOutOfRange.Add(1, typeTag);

        _registrationDuration.Record(elapsed.TotalSeconds, typeTag);
    }

    public void RecordRejected(string reason, VitalSignType? type)
    {
        var reasonTag = new KeyValuePair<string, object?>("reason", reason);

        if (type is { } knownType && Enum.IsDefined(knownType))
            _readingsRejected.Add(1, reasonTag, TypeTag(knownType));
        else
            _readingsRejected.Add(1, reasonTag);
    }

    public void RecordAlertRaised(AlertSeverity severity, VitalSignType type)
    {
        _alertsRaised.Add(1, new KeyValuePair<string, object?>("severity", severity.ToString()), TypeTag(type));
    }

    private static KeyValuePair<string, object?> TypeTag(VitalSignType type) => new("vital_sign_type", type.ToString());
}
