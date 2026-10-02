namespace VitaSignal.DeviceSimulator;

internal sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public Uri ApiBaseUrl { get; set; } = new("http://localhost:8080");
    public int Patients { get; set; } = 10;
    public double IntervalSeconds { get; set; } = 5;
    public double AnomalyRate { get; set; } = 0.1;

    public bool IsValid() =>
        Patients is > 0 and <= 500
        && IntervalSeconds >= 0.5
        && AnomalyRate is >= 0 and <= 1;
}
