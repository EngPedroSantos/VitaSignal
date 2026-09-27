using VitaSignal.Application.Patients;
using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Application.VitalReadings;

public sealed class RegisterVitalReadingUseCase
{
    private readonly IPatientRepository _patientRepository;
    private readonly IVitalReadingRepository _vitalReadingRepository;

    public RegisterVitalReadingUseCase(
        IPatientRepository patientRepository,
        IVitalReadingRepository vitalReadingRepository)
    {
        _patientRepository = patientRepository;
        _vitalReadingRepository = vitalReadingRepository;
    }

    public async Task<RegisterVitalReadingResult> ExecuteAsync(RegisterVitalReadingRequest request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            throw new PatientNotFoundException(request.PatientId);

        var reading = VitalReading.Create(request.PatientId, request.Type, request.Value, request.RecordedAtUtc, request.DeviceId);

        await _vitalReadingRepository.AddAsync(reading, cancellationToken);

        var normalRange = VitalRangeCatalog.GetNormalRange(request.Type);
        var isWithinRange = normalRange.Contains(request.Value);

        return new RegisterVitalReadingResult(reading, isWithinRange);
    }
}