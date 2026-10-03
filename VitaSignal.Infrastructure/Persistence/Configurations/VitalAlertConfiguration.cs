using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.Patients;
using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Infrastructure.Persistence.Configurations;

public sealed class VitalAlertConfiguration : IEntityTypeConfiguration<VitalAlert>
{
    public void Configure(EntityTypeBuilder<VitalAlert> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Value);
        builder.Property(a => a.RaisedAtUtc);
        builder.Property(a => a.AcknowledgedAtUtc);
        builder.Ignore(a => a.IsAcknowledged);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<VitalReading>()
            .WithOne()
            .HasForeignKey<VitalAlert>(a => a.ReadingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.PatientId, a.RaisedAtUtc, a.Id })
            .IsDescending(false, true, true);

        builder.HasIndex(a => new { a.PatientId, a.RaisedAtUtc })
            .HasFilter("\"AcknowledgedAtUtc\" IS NULL")
            .HasDatabaseName("IX_VitalAlerts_Pending");
    }
}
