using System.Collections.Concurrent;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Infrastructure.VitalReadings;

public sealed class InMemoryVitalReadingRepository : IVitalReadingRepository
{
    private readonly ConcurrentDictionary<Guid, VitalReading> _readings = new();

    public Task AddAsync(VitalReading reading, CancellationToken cancellationToken)
    {
        _readings[reading.Id] = reading;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VitalReading>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var result = _readings.Values.Where(r => r.PatientId == patientId).ToList();
        return Task.FromResult<IReadOnlyList<VitalReading>>(result);
    }
}