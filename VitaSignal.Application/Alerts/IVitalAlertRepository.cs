using VitaSignal.Application.Common.Pagination;
using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.Alerts.Enums;

namespace VitaSignal.Application.Alerts;

public interface IVitalAlertRepository
{
    Task AddAsync(VitalAlert alert, CancellationToken cancellationToken);
    Task<VitalAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<VitalAlert>> GetPageByPatientIdAsync(
        Guid patientId, AlertSeverity? severity, bool pendingOnly, KeysetCursor? after, int take, CancellationToken cancellationToken);
}
