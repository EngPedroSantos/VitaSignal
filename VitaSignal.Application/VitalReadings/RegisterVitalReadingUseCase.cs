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

        if (request.Type is null)
            throw new ArgumentException("Vital sign type is required.", nameof(request.Type));

        if (request.RecordedAtUtc is null)
            throw new ArgumentException("Recorded time is required.", nameof(request.RecordedAtUtc));

        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            throw new PatientNotFoundException(request.PatientId);

        var reading = VitalReading.Create(request.PatientId, request.Type.Value, request.Value, request.RecordedAtUtc.Value, request.DeviceId);

        await _vitalReadingRepository.AddAsync(reading, cancellationToken);

        var normalRange = VitalRangeCatalog.GetNormalRange(reading.Type);
        var isWithinRange = normalRange.Contains(reading.Value);

        RecordMetrics(reading.Type, isWithinRange, stopwatch.Elapsed);

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