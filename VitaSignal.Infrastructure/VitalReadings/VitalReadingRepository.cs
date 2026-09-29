using Microsoft.EntityFrameworkCore;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Domain.VitalReadings;
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
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VitalReading>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        return await _dbContext.VitalReadings
            .Where(r => r.PatientId == patientId)
            .ToListAsync(cancellationToken);
    }
}