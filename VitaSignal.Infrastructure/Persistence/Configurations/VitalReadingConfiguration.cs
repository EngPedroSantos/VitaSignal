using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Infrastructure.Persistence.Configurations;

public sealed class VitalReadingConfiguration : IEntityTypeConfiguration<VitalReading>
{
    public void Configure(EntityTypeBuilder<VitalReading> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.PatientId);
        builder.Property(r => r.Value);
        builder.Property(r => r.RecordedAtUtc);
        builder.Property(r => r.DeviceId).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Unit).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(30);
    }
}