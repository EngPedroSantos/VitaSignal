using VitaSignal.Application.Common.Pagination;
using VitaSignal.Application.Patients;
using VitaSignal.Application.Patients.Exceptions;
using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.Alerts.Enums;

namespace VitaSignal.Application.Alerts;

public sealed class ListPatientAlertsUseCase
{
    private readonly IPatientRepository _patientRepository;
    private readonly IVitalAlertRepository _vitalAlertRepository;

    public ListPatientAlertsUseCase(IPatientRepository patientRepository, IVitalAlertRepository vitalAlertRepository)
    {
        _patientRepository = patientRepository;
        _vitalAlertRepository = vitalAlertRepository;
    }

    public async Task<Page<VitalAlert>> ExecuteAsync(
        Guid patientId, AlertSeverity? severity, bool pendingOnly, string? cursor, int? limit, CancellationToken cancellationToken)
    {
        var resolvedLimit = Paging.ResolveLimit(limit);
        var after = KeysetCursor.Decode(cursor);

        if (!await _patientRepository.ExistsAsync(patientId, cancellationToken))
            throw new PatientNotFoundException(patientId);

        var rows = await _vitalAlertRepository.GetPageByPatientIdAsync(
            patientId, severity, pendingOnly, after, resolvedLimit + 1, cancellationToken);

        return Paging.ToPage(rows, resolvedLimit, a => new KeysetCursor(a.RaisedAtUtc, a.Id));
    }
}
