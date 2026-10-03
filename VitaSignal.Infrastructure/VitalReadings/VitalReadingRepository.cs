using Microsoft.EntityFrameworkCore;
using VitaSignal.Application.Common.Pagination;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;
using VitaSignal.Infrastructure.Persistence;

namespace VitaSignal.Infrastructure.VitalReadings;

public sealed class VitalReadingRepository : IVitalReadingRepository
{
    private readonly VitaSignalDbContext _dbContext;

    public VitalReadingRepository(VitaSignalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(VitalReading reading, CancellationToken cancellationToken)
    {
        await _dbContext.VitalReadings.AddAsync(reading, cancellationToken);
    }

    public async Task<VitalReading?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.VitalReadings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<VitalReading>> GetPageByPatientIdAsync(
        Guid patientId, VitalSignType? type, KeysetCursor? after, int take, CancellationToken cancellationToken)
    {
        var query = _dbContext.VitalReadings
            .AsNoTracking()
            .Where(r => r.PatientId == patientId);

        if (type is not null)
            query = query.Where(r => r.Type == type);

        if (after is { } cursor)
        {
            query = query.Where(r => EF.Functions.LessThan(
                ValueTuple.Create(r.RecordedAtUtc, r.Id),
                ValueTuple.Create(cursor.Timestamp, cursor.Id)));
        }

        return await query
            .OrderByDescending(r => r.RecordedAtUtc)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
