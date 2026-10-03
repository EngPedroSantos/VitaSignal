using Microsoft.Extensions.Logging;
using VitaSignal.Application.Common;
using VitaSignal.Application.Common.Diagnostics;
using VitaSignal.Domain.Patients;

namespace VitaSignal.Application.Patients;

public sealed partial class RegisterPatientUseCase
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterPatientUseCase> _logger;

    public RegisterPatientUseCase(
        IPatientRepository patientRepository,
        IUnitOfWork unitOfWork,
        ILogger<RegisterPatientUseCase> logger)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Patient> ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("RegisterPatient");

        var patient = Patient.Create();

        await _patientRepository.AddAsync(patient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        activity?.SetTag("vitasignal.patient_id", patient.Id);
        LogPatientRegistered(_logger, patient.Id, patient.Code);

        return patient;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Patient {PatientId} registered with code {PatientCode}")]
    private static partial void LogPatientRegistered(ILogger logger, Guid patientId, string patientCode);
}
