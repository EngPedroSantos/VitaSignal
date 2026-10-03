using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VitaSignal.Domain.Patients;

namespace VitaSignal.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).IsRequired().HasMaxLength(Patient.CodeLength);
        builder.HasIndex(p => p.Code).IsUnique();
    }
}
