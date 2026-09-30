using System.Diagnostics.Metrics;

namespace VitaSignal.Application.VitalReadings;

public static class VitalReadingMetrics
{
    private static readonly Meter Meter = new("VitaSignal.Application");

    public static readonly Counter<long> ReadingsRegistered = Meter.CreateCounter<long>(
        "vitasignal.vital_readings.registered",
        description: "Total vital sign readings recorded, by type.");

    public static readonly Counter<long> ReadingsOutOfRange = Meter.CreateCounter<long>(
        "vitasignal.vital_readings.out_of_range",
        description: "Total readings recorded outside the normal range, by type.");

    public static readonly Histogram<double> RegistrationDuration = Meter.CreateHistogram<double>(
        "vitasignal.vital_readings.registration_duration",
        unit: "s",
        description: "Duration of vital sign reading record processing.",
        advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5]
        });
}