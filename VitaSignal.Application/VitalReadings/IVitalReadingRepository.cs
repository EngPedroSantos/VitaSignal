using VitaSignal.Application.Common.Pagination;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Application.VitalReadings;

public interface IVitalReadingRepository
{
    Task AddAsync(VitalReading reading, CancellationToken cancellationToken);
    Task<VitalReading?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<VitalReading>> GetPageByPatientIdAsync(
        Guid patientId, VitalSignType? type, KeysetCursor? after, int take, CancellationToken cancellationToken);
}
