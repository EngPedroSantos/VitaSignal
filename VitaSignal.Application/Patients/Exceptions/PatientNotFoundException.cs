using VitaSignal.Application.Common.Exceptions;

namespace VitaSignal.Application.Patients;

public sealed class PatientNotFoundException : NotFoundException
{
    public Guid PatientId { get; }

    public PatientNotFoundException(Guid patientId)
        : base($"Patient '{patientId}' was not found.")
    {
        PatientId = patientId;
    }
}