using System.Diagnostics;
using Microsoft.Extensions.Logging;
using VitaSignal.Application.Alerts;
using VitaSignal.Application.Common;
using VitaSignal.Application.Common.Diagnostics;
using VitaSignal.Application.Common.Exceptions;
using VitaSignal.Application.Patients;
using VitaSignal.Application.Patients.Exceptions;
using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.Alerts.Enums;
using VitaSignal.Domain.Common;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Application.VitalReadings;

public sealed partial class RegisterVitalReadingUseCase
{
    private readonly IPatientRepository _patientRepository;
    private readonly IVitalReadingRepository _vitalReadingRepository;
    private readonly IVitalAlertRepository _vitalAlertRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly VitalReadingMetrics _metrics;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RegisterVitalReadingUseCase> _logger;

    public RegisterVitalReadingUseCase(
        IPatientRepository patientRepository,
        IVitalReadingRepository vitalReadingRepository,
        IVitalAlertRepository vitalAlertRepository,
        IUnitOfWork unitOfWork,
        VitalReadingMetrics metrics,
        TimeProvider timeProvider,
        ILogger<RegisterVitalReadingUseCase> logger)
    {
        _patientRepository = patientRepository;
        _vitalReadingRepository = vitalReadingRepository;
        _vitalAlertRepository = vitalAlertRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<RegisterVitalReadingResult> ExecuteAsync(RegisterVitalReadingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("RegisterVitalReading");
        activity?.SetTag("vitasignal.patient_id", request.PatientId);
        activity?.SetTag("vital_sign_type", request.Type?.ToString());

        var startedAt = _timeProvider.GetTimestamp();

        try
        {
            var result = await RegisterAsync(request, cancellationToken);

            _metrics.RecordRegistered(result.Reading.Type, result.IsWithinNormalRange, _timeProvider.GetElapsedTime(startedAt));
            activity?.SetTag("vitasignal.within_normal_range", result.IsWithinNormalRange);

            return result;
        }
        catch (Exception ex) when (RejectionReasonFor(ex) is { } reason)
        {
            _metrics.RecordRejected(reason, request.Type);
            activity?.SetTag("vitasignal.rejection_reason", reason);
            LogReadingRejected(_logger, request.PatientId, reason, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private async Task<RegisterVitalReadingResult> RegisterAsync(RegisterVitalReadingRequest request, CancellationToken cancellationToken)
    {
        if (request.Type is null)
            throw new InvalidRequestException("Vital sign type is required.");

        if (request.RecordedAtUtc is null)
            throw new InvalidRequestException("Recorded time is required.");

        if (!await _patientRepository.ExistsAsync(request.PatientId, cancellationToken))
            throw new PatientNotFoundException(request.PatientId);

        var now = _timeProvider.GetUtcNow();
        var reading = VitalReading.Create(
            request.PatientId, request.Type.Value, request.Value, request.RecordedAtUtc.Value, request.DeviceId, now);
        var alert = VitalAlert.RaiseIfNeeded(reading, now);

        await _vitalReadingRepository.AddAsync(reading, cancellationToken);
        if (alert is not null)
            await _vitalAlertRepository.AddAsync(alert, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogReadingRegistered(_logger, reading.Id, reading.PatientId, reading.Type, reading.Value, reading.DeviceId);

        if (alert is not null)
        {
            _metrics.RecordAlertRaised(alert.Severity, alert.Type);

            if (alert.Severity == AlertSeverity.Critical)
                LogCriticalAlertRaised(_logger, alert.Id, alert.PatientId, alert.Type, alert.Value);
            else
                LogWarningAlertRaised(_logger, alert.Id, alert.PatientId, alert.Type, alert.Value);
        }

        return new RegisterVitalReadingResult(reading, alert is null, alert);
    }

    private static string? RejectionReasonFor(Exception exception) => exception switch
    {
        PatientNotFoundException => RejectionReasons.PatientNotFound,
        ImplausibleVitalValueException => RejectionReasons.ImplausibleValue,
        DomainValidationException or InvalidRequestException => RejectionReasons.ValidationFailed,
        _ => null
    };

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Reading {ReadingId} registered for patient {PatientId}: {VitalSignType} = {Value} (device {DeviceId})")]
    private static partial void LogReadingRegistered(
        ILogger logger, Guid readingId, Guid patientId, VitalSignType vitalSignType, double value, string deviceId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Reading for patient {PatientId} rejected ({RejectionReason}): {RejectionDetail}")]
    private static partial void LogReadingRejected(ILogger logger, Guid patientId, string rejectionReason, string rejectionDetail);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Warning alert {AlertId} raised for patient {PatientId}: {VitalSignType} = {Value}")]
    private static partial void LogWarningAlertRaised(ILogger logger, Guid alertId, Guid patientId, VitalSignType vitalSignType, double value);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Critical alert {AlertId} raised for patient {PatientId}: {VitalSignType} = {Value}")]
    private static partial void LogCriticalAlertRaised(ILogger logger, Guid alertId, Guid patientId, VitalSignType vitalSignType, double value);
}
