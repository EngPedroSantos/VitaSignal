using System.Diagnostics;
using VitaSignal.Application.Patients;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

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
        var stopwatch = Stopwatch.StartNew();

        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            throw new PatientNotFoundException(request.PatientId);

        var reading = VitalReading.Create(request.PatientId, request.Type, request.Value, request.RecordedAtUtc, request.DeviceId);

        await _vitalReadingRepository.AddAsync(reading, cancellationToken);

        var normalRange = VitalRangeCatalog.GetNormalRange(request.Type);
        var isWithinRange = normalRange.Contains(request.Value);

        RecordMetrics(request.Type, isWithinRange, stopwatch.Elapsed);

        return new RegisterVitalReadingResult(reading, isWithinRange);
    }

    private static void RecordMetrics(VitalSignType type, bool isWithinRange, TimeSpan elapsed)
    {
        var typeTag = new KeyValuePair<string, object?>("vital_sign_type", type.ToString());

        VitalReadingMetrics.ReadingsRegistered.Add(1, typeTag);
        if (!isWithinRange)
            VitalReadingMetrics.ReadingsOutOfRange.Add(1, typeTag);

        VitalReadingMetrics.RegistrationDuration.Record(elapsed.TotalSeconds, typeTag);
    }
}