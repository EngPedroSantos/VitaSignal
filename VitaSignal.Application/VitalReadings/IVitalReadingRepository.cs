using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Application.VitalReadings;

public interface IVitalReadingRepository
{
    Task AddAsync(VitalReading reading, CancellationToken cancellationToken);
    Task<IReadOnlyList<VitalReading>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);
}