using VitaSignal.Application.Common.Pagination;
using VitaSignal.Application.Patients;
using VitaSignal.Application.Patients.Exceptions;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Application.VitalReadings;

public sealed class ListPatientReadingsUseCase
{
    private readonly IPatientRepository _patientRepository;
    private readonly IVitalReadingRepository _vitalReadingRepository;

    public ListPatientReadingsUseCase(IPatientRepository patientRepository, IVitalReadingRepository vitalReadingRepository)
    {
        _patientRepository = patientRepository;
        _vitalReadingRepository = vitalReadingRepository;
    }

    public async Task<Page<VitalReading>> ExecuteAsync(
        Guid patientId, VitalSignType? type, string? cursor, int? limit, CancellationToken cancellationToken)
    {
        var resolvedLimit = Paging.ResolveLimit(limit);
        var after = KeysetCursor.Decode(cursor);

        if (!await _patientRepository.ExistsAsync(patientId, cancellationToken))
            throw new PatientNotFoundException(patientId);

        var rows = await _vitalReadingRepository.GetPageByPatientIdAsync(
            patientId, type, after, resolvedLimit + 1, cancellationToken);

        return Paging.ToPage(rows, resolvedLimit, r => new KeysetCursor(r.RecordedAtUtc, r.Id));
    }
}
