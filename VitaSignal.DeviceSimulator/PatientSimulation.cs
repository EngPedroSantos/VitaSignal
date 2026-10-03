namespace VitaSignal.DeviceSimulator;

internal sealed class PatientSimulation
{
    private const double Smoothing = 0.25;

    private static readonly Scenario[] AnomalousScenarios =
        [Scenario.Tachycardia, Scenario.Fever, Scenario.Desaturation, Scenario.Hypertension];

    private static readonly Dictionary<VitalSign, (double Min, double Max, int Decimals)> Limits = new()
    {
        [VitalSign.HeartRate] = (30, 220, 0),
        [VitalSign.SpO2] = (70, 100, 0),
        [VitalSign.BodyTemperature] = (34, 42, 1),
        [VitalSign.SystolicBloodPressure] = (60, 240, 0),
        [VitalSign.DiastolicBloodPressure] = (35, 150, 0)
    };

    private readonly Random _random;
    private readonly Dictionary<VitalSign, double> _baseline;
    private readonly Dictionary<VitalSign, double> _current;
    private int _episodeTicksLeft;

    public Guid PatientId { get; }
    public string DeviceId { get; }
    public Scenario Scenario { get; private set; } = Scenario.Stable;

    public PatientSimulation(Guid patientId, string deviceId, Random random)
    {
        PatientId = patientId;
        DeviceId = deviceId;
        _random = random;

        _baseline = new Dictionary<VitalSign, double>
        {
            [VitalSign.HeartRate] = Gaussian(72, 6),
            [VitalSign.SpO2] = Gaussian(97.5, 0.8),
            [VitalSign.BodyTemperature] = Gaussian(36.7, 0.2),
            [VitalSign.SystolicBloodPressure] = Gaussian(108, 5),
            [VitalSign.DiastolicBloodPressure] = Gaussian(70, 3)
        };

        _current = new Dictionary<VitalSign, double>(_baseline);
    }

    public IReadOnlyList<SimulatedReading> Tick(double anomalyRate)
    {
        UpdateScenario(anomalyRate);

        var readings = new List<SimulatedReading>(_current.Count);

        foreach (var sign in Enum.GetValues<VitalSign>())
        {
            var target = _baseline[sign] + OffsetFor(sign);
            var next = _current[sign] + ((target - _current[sign]) * Smoothing) + Gaussian(0, NoiseFor(sign));
            var (min, max, decimals) = Limits[sign];

            _current[sign] = Math.Clamp(next, min, max);
            readings.Add(new SimulatedReading(sign, Math.Round(_current[sign], decimals)));
        }

        return readings;
    }

    private void UpdateScenario(double anomalyRate)
    {
        if (Scenario != Scenario.Stable)
        {
            if (--_episodeTicksLeft <= 0)
                Scenario = Scenario.Stable;

            return;
        }

        if (_random.NextDouble() < anomalyRate / 12)
        {
            Scenario = AnomalousScenarios[_random.Next(AnomalousScenarios.Length)];
            _episodeTicksLeft = _random.Next(6, 19);
        }
    }

    private double OffsetFor(VitalSign sign) => (Scenario, sign) switch
    {
        (Scenario.Tachycardia, VitalSign.HeartRate) => 65,
        (Scenario.Fever, VitalSign.BodyTemperature) => 2.6,
        (Scenario.Fever, VitalSign.HeartRate) => 22,
        (Scenario.Desaturation, VitalSign.SpO2) => -11,
        (Scenario.Desaturation, VitalSign.HeartRate) => 15,
        (Scenario.Hypertension, VitalSign.SystolicBloodPressure) => 70,
        (Scenario.Hypertension, VitalSign.DiastolicBloodPressure) => 35,
        _ => 0
    };

    private static double NoiseFor(VitalSign sign) => sign switch
    {
        VitalSign.HeartRate => 1.5,
        VitalSign.SpO2 => 0.3,
        VitalSign.BodyTemperature => 0.03,
        _ => 1.2
    };

    private double Gaussian(double mean, double standardDeviation)
    {
        var u1 = 1.0 - _random.NextDouble();
        var u2 = _random.NextDouble();
        var standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

        return mean + (standardDeviation * standardNormal);
    }
}
