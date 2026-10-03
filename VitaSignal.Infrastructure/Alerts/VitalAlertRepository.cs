using Microsoft.EntityFrameworkCore;
using VitaSignal.Application.Alerts;
using VitaSignal.Application.Common.Pagination;
using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.Alerts.Enums;
using VitaSignal.Infrastructure.Persistence;

namespace VitaSignal.Infrastructure.Alerts;

public sealed class VitalAlertRepository : IVitalAlertRepository
{
    private readonly VitaSignalDbContext _dbContext;

    public VitalAlertRepository(VitaSignalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(VitalAlert alert, CancellationToken cancellationToken)
    {
        await _dbContext.VitalAlerts.AddAsync(alert, cancellationToken);
    }

    public async Task<VitalAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.VitalAlerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<VitalAlert>> GetPageByPatientIdAsync(
        Guid patientId, AlertSeverity? severity, bool pendingOnly, KeysetCursor? after, int take, CancellationToken cancellationToken)
    {
        var query = _dbContext.VitalAlerts
            .AsNoTracking()
            .Where(a => a.PatientId == patientId);

        if (severity is not null)
            query = query.Where(a => a.Severity == severity);

        if (pendingOnly)
            query = query.Where(a => a.AcknowledgedAtUtc == null);

        if (after is { } cursor)
        {
            query = query.Where(a => EF.Functions.LessThan(
                ValueTuple.Create(a.RaisedAtUtc, a.Id),
                ValueTuple.Create(cursor.Timestamp, cursor.Id)));
        }

        return await query
            .OrderByDescending(a => a.RaisedAtUtc)
            .ThenByDescending(a => a.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
