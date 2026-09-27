using System.Collections.Concurrent;
using VitaSignal.Application.Patients;
using VitaSignal.Domain.Patients;

namespace VitaSignal.Infrastructure.Patients;

public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly ConcurrentDictionary<Guid, Patient> _patients = new();

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _patients.TryGetValue(id, out var patient);
        return Task.FromResult(patient);
    }

    public Task AddAsync(Patient patient, CancellationToken cancellationToken)
    {
        _patients[patient.Id] = patient;
        return Task.CompletedTask;
    }
}