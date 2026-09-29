using Microsoft.EntityFrameworkCore;
using VitaSignal.Domain.Patients;
using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Infrastructure.Persistence;

public sealed class VitaSignalDbContext : DbContext
{
    public VitaSignalDbContext(DbContextOptions<VitaSignalDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<VitalReading> VitalReadings => Set<VitalReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VitaSignalDbContext).Assembly);
    }
}