using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace VitaSignal.DeviceSimulator;

internal sealed partial class DeviceSimulatorWorker : BackgroundService
{
    public const string ActivitySourceName = "VitaSignal.DeviceSimulator";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private readonly VitaSignalApiClient _apiClient;
    private readonly SimulatorOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DeviceSimulatorWorker> _logger;
    private readonly Random _random = new();

    public DeviceSimulatorWorker(
        VitaSignalApiClient apiClient,
        IOptions<SimulatorOptions> options,
        TimeProvider timeProvider,
        ILogger<DeviceSimulatorWorker> logger)
    {
        _apiClient = apiClient;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var patients = await RegisterPatientsAsync(stoppingToken);

        LogSimulationStarted(_logger, patients.Count, _options.IntervalSeconds, _options.AnomalyRate);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds), _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await Task.WhenAll(patients.Select(patient => SendTickAsync(patient, stoppingToken)));
        }
    }

    private async Task<List<PatientSimulation>> RegisterPatientsAsync(CancellationToken stoppingToken)
    {
        var patients = new List<PatientSimulation>(_options.Patients);

        for (var index = 1; index <= _options.Patients; index++)
        {
            var patientId = await _apiClient.RegisterPatientAsync(stoppingToken);
            var deviceId = string.Create(CultureInfo.InvariantCulture, $"monitor-{index:D3}");

            patients.Add(new PatientSimulation(patientId, deviceId, new Random(_random.Next())));
        }

        return patients;
    }

    private async Task SendTickAsync(PatientSimulation patient, CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("DeviceTick");
        activity?.SetTag("vitasignal.patient_id", patient.PatientId);
        activity?.SetTag("vitasignal.device_id", patient.DeviceId);

        var scenarioBefore = patient.Scenario;
        var readings = patient.Tick(_options.AnomalyRate);
        activity?.SetTag("vitasignal.scenario", patient.Scenario.ToString());

        if (patient.Scenario != scenarioBefore)
            LogScenarioChanged(_logger, patient.DeviceId, scenarioBefore, patient.Scenario);

        var recordedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var reading in readings)
        {
            try
            {
                var statusCode = await _apiClient.SendReadingAsync(
                    patient.PatientId, reading, recordedAtUtc, patient.DeviceId, stoppingToken);

                if (statusCode >= 400)
                    LogReadingRejected(_logger, patient.DeviceId, reading.Type, reading.Value, statusCode);
            }
            catch (HttpRequestException ex)
            {
                LogApiUnavailable(_logger, ex, patient.DeviceId);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                return;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Simulating {PatientCount} patients every {IntervalSeconds}s (anomaly rate {AnomalyRate})")]
    private static partial void LogSimulationStarted(ILogger logger, int patientCount, double intervalSeconds, double anomalyRate);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device {DeviceId} changed from {PreviousScenario} to {Scenario}")]
    private static partial void LogScenarioChanged(ILogger logger, string deviceId, Scenario previousScenario, Scenario scenario);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "API rejected reading from {DeviceId}: {VitalSign} = {Value} (HTTP {StatusCode})")]
    private static partial void LogReadingRejected(ILogger logger, string deviceId, VitalSign vitalSign, double value, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API unavailable while sending readings from {DeviceId}")]
    private static partial void LogApiUnavailable(ILogger logger, Exception exception, string deviceId);
}
