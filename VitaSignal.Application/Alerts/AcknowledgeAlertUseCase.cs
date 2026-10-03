using Microsoft.Extensions.Logging;
using VitaSignal.Application.Alerts.Exceptions;
using VitaSignal.Application.Common;
using VitaSignal.Application.Common.Diagnostics;
using VitaSignal.Domain.Alerts;

namespace VitaSignal.Application.Alerts;

public sealed partial class AcknowledgeAlertUseCase
{
    private readonly IVitalAlertRepository _vitalAlertRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AcknowledgeAlertUseCase> _logger;

    public AcknowledgeAlertUseCase(
        IVitalAlertRepository vitalAlertRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<AcknowledgeAlertUseCase> logger)
    {
        _vitalAlertRepository = vitalAlertRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<VitalAlert> ExecuteAsync(Guid alertId, CancellationToken cancellationToken)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("AcknowledgeAlert");
        activity?.SetTag("vitasignal.alert_id", alertId);

        var alert = await _vitalAlertRepository.GetByIdAsync(alertId, cancellationToken)
            ?? throw new AlertNotFoundException(alertId);

        if (alert.IsAcknowledged)
            return alert;

        alert.Acknowledge(_timeProvider.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogAlertAcknowledged(_logger, alert.Id, alert.PatientId, alert.AcknowledgedAtUtc!.Value - alert.RaisedAtUtc);

        return alert;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Alert {AlertId} for patient {PatientId} acknowledged after {TimeToAcknowledge}")]
    private static partial void LogAlertAcknowledged(ILogger logger, Guid alertId, Guid patientId, TimeSpan timeToAcknowledge);
}
