using VitaSignal.Domain.Patients;

namespace VitaSignal.Application.Patients;

public sealed class RegisterPatientUseCase
{
    private readonly IPatientRepository _patientRepository;

    public RegisterPatientUseCase(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Patient> ExecuteAsync(RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        var patient = Patient.Create(request.DisplayName);

        await _patientRepository.AddAsync(patient, cancellationToken);

        return patient;
    }
}