using VitaSignal.Domain.Patients;

namespace VitaSignal.Application.Patients
{
    public interface IPatientRepository
    {
        Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}