using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.Alerts.Enums;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.Tests.Alerts;

public class VitalAlertTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private static VitalReading CreateReading(VitalSignType type, double value) =>
        VitalReading.Create(Guid.NewGuid(), type, value, Now.UtcDateTime.AddMinutes(-1), "device123", Now);

    [Theory]
    [InlineData(VitalSignType.HeartRate, 75)]
    [InlineData(VitalSignType.HeartRate, 60)]
    [InlineData(VitalSignType.HeartRate, 100)]
    [InlineData(VitalSignType.SpO2, 97)]
    [InlineData(VitalSignType.BodyTemperature, 36.6)]
    public void Should_NotRaiseAlert_When_ValueIsWithinNormalRange(VitalSignType type, double value)
    {
        var alert = VitalAlert.RaiseIfNeeded(CreateReading(type, value), Now);

        Assert.Null(alert);
    }

    [Theory]
    [InlineData(VitalSignType.HeartRate, 110)]
    [InlineData(VitalSignType.HeartRate, 50)]
    [InlineData(VitalSignType.SpO2, 92)]
    [InlineData(VitalSignType.BodyTemperature, 38)]
    [InlineData(VitalSignType.SystolicBloodPressure, 140)]
    [InlineData(VitalSignType.HeartRate, 130)]
    public void Should_RaiseWarning_When_ValueIsOutsideNormalButInsideCriticalRange(VitalSignType type, double value)
    {
        var alert = VitalAlert.RaiseIfNeeded(CreateReading(type, value), Now);

        Assert.NotNull(alert);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
    }

    [Theory]
    [InlineData(VitalSignType.HeartRate, 180)]
    [InlineData(VitalSignType.HeartRate, 35)]
    [InlineData(VitalSignType.SpO2, 85)]
    [InlineData(VitalSignType.BodyTemperature, 40)]
    [InlineData(VitalSignType.SystolicBloodPressure, 200)]
    [InlineData(VitalSignType.DiastolicBloodPressure, 45)]
    public void Should_RaiseCritical_When_ValueIsOutsideCriticalRange(VitalSignType type, double value)
    {
        var alert = VitalAlert.RaiseIfNeeded(CreateReading(type, value), Now);

        Assert.NotNull(alert);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
    }

    [Fact]
    public void Should_CopyReadingData_When_AlertIsRaised()
    {
        var reading = CreateReading(VitalSignType.HeartRate, 180);

        var alert = VitalAlert.RaiseIfNeeded(reading, Now);

        Assert.NotNull(alert);
        Assert.Equal(reading.Id, alert.ReadingId);
        Assert.Equal(reading.PatientId, alert.PatientId);
        Assert.Equal(reading.Type, alert.Type);
        Assert.Equal(reading.Value, alert.Value);
        Assert.Equal(Now.UtcDateTime, alert.RaisedAtUtc);
        Assert.False(alert.IsAcknowledged);
    }

    [Fact]
    public void Should_SetAcknowledgedAt_When_AlertIsAcknowledged()
    {
        var alert = VitalAlert.RaiseIfNeeded(CreateReading(VitalSignType.HeartRate, 180), Now)!;
        var acknowledgedAt = Now.AddMinutes(3);

        alert.Acknowledge(acknowledgedAt);

        Assert.True(alert.IsAcknowledged);
        Assert.Equal(acknowledgedAt.UtcDateTime, alert.AcknowledgedAtUtc);
    }

    [Fact]
    public void Should_KeepFirstAcknowledgedAt_When_AlertIsAcknowledgedTwice()
    {
        var alert = VitalAlert.RaiseIfNeeded(CreateReading(VitalSignType.HeartRate, 180), Now)!;
        var firstAcknowledgement = Now.AddMinutes(3);

        alert.Acknowledge(firstAcknowledgement);
        alert.Acknowledge(Now.AddMinutes(10));

        Assert.Equal(firstAcknowledgement.UtcDateTime, alert.AcknowledgedAtUtc);
    }
}
