using Microsoft.EntityFrameworkCore;
using VitaSignal.Application.Patients;
using VitaSignal.Domain.Patients;
using VitaSignal.Infrastructure.Persistence;

namespace VitaSignal.Infrastructure.Patients;

public sealed class PatientRepository : IPatientRepository
{
    private readonly VitaSignalDbContext _dbContext;

    public PatientRepository(VitaSignalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken)
    {
        await _dbContext.Patients.AddAsync(patient, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}