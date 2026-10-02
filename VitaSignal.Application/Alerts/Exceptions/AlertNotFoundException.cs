using VitaSignal.Application.Common.Exceptions;

namespace VitaSignal.Application.Alerts.Exceptions;

public sealed class AlertNotFoundException : NotFoundException
{
    public Guid AlertId { get; }

    public AlertNotFoundException(Guid alertId)
        : base($"Alert '{alertId}' was not found.")
    {
        AlertId = alertId;
    }
}
