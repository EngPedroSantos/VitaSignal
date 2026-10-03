using VitaSignal.Domain.Common;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.Tests.VitalReadings;

public class VitalReadingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime FiveMinutesAgo = Now.UtcDateTime.AddMinutes(-5);

    private static VitalReading CreateReading(
        VitalSignType type = VitalSignType.HeartRate,
        double value = 70,
        DateTime? recordedAt = null,
        string deviceId = "device123",
        Guid? patientId = null) =>
        VitalReading.Create(patientId ?? Guid.NewGuid(), type, value, recordedAt ?? FiveMinutesAgo, deviceId, Now);

    [Theory]
    [InlineData(VitalSignType.HeartRate, 70, "bpm")]
    [InlineData(VitalSignType.SpO2, 98, "%")]
    [InlineData(VitalSignType.BodyTemperature, 36.5, "°C")]
    [InlineData(VitalSignType.SystolicBloodPressure, 110, "mmHg")]
    [InlineData(VitalSignType.DiastolicBloodPressure, 70, "mmHg")]
    public void Should_CreateVitalReading_When_DataIsValid(VitalSignType type, double value, string expectedUnit)
    {
        var vitalReading = CreateReading(type, value);

        Assert.Equal(expectedUnit, vitalReading.Unit);
        Assert.Equal(type, vitalReading.Type);
    }

    [Fact]
    public void Should_ThrowDomainValidationException_When_PatientIdIsEmpty()
    {
        Assert.Throws<DomainValidationException>(() => CreateReading(patientId: Guid.Empty));
    }

    [Fact]
    public void Should_ThrowDomainValidationException_When_TypeIsNotDefined()
    {
        Assert.Throws<DomainValidationException>(() => CreateReading(type: (VitalSignType)999));
    }

    [Theory]
    [InlineData(VitalSignType.SpO2, 250)]
    [InlineData(VitalSignType.SpO2, 0)]
    [InlineData(VitalSignType.HeartRate, -1)]
    [InlineData(VitalSignType.HeartRate, 400)]
    [InlineData(VitalSignType.HeartRate, 10)]
    [InlineData(VitalSignType.BodyTemperature, 70)]
    [InlineData(VitalSignType.BodyTemperature, 20)]
    [InlineData(VitalSignType.SystolicBloodPressure, 350)]
    [InlineData(VitalSignType.DiastolicBloodPressure, 10)]
    public void Should_ThrowImplausibleVitalValueException_When_ValueIsNotPlausible(VitalSignType type, double value)
    {
        var exception = Assert.Throws<ImplausibleVitalValueException>(() => CreateReading(type, value));

        Assert.Equal(type, exception.Type);
        Assert.Equal(value, exception.Value);
    }

    [Fact]
    public void Should_ThrowImplausibleVitalValueException_When_ValueIsNaN()
    {
        Assert.Throws<ImplausibleVitalValueException>(() => CreateReading(value: double.NaN));
    }

    [Theory]
    [InlineData(VitalSignType.SpO2, 100)]
    [InlineData(VitalSignType.HeartRate, 20)]
    [InlineData(VitalSignType.HeartRate, 300)]
    [InlineData(VitalSignType.BodyTemperature, 45)]
    public void Should_CreateVitalReading_When_ValueIsAtPlausibleLimit(VitalSignType type, double value)
    {
        var vitalReading = CreateReading(type, value);

        Assert.Equal(value, vitalReading.Value);
    }

    [Fact]
    public void Should_CreateVitalReading_When_ValueIsOutsideNormalRangeButPlausible()
    {
        var vitalReading = CreateReading(value: 180);

        Assert.Equal(180, vitalReading.Value);
    }

    [Fact]
    public void Should_ThrowDomainValidationException_When_RecordedAtIsMoreThanOneMinuteInTheFuture()
    {
        var recordedAt = Now.UtcDateTime.AddSeconds(61);

        Assert.Throws<DomainValidationException>(() => CreateReading(recordedAt: recordedAt));
    }

    [Fact]
    public void Should_CreateVitalReading_When_RecordedAtIsWithinClockSkewTolerance()
    {
        var recordedAt = Now.UtcDateTime.AddSeconds(59);

        var vitalReading = CreateReading(recordedAt: recordedAt);

        Assert.Equal(recordedAt, vitalReading.RecordedAtUtc);
    }

    [Fact]
    public void Should_ThrowDomainValidationException_When_RecordedAtHasNoTimezone()
    {
        var recordedAt = new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Unspecified);

        Assert.Throws<DomainValidationException>(() => CreateReading(recordedAt: recordedAt));
    }

    [Fact]
    public void Should_ThrowDomainValidationException_When_RecordedAtIsDefault()
    {
        var recordedAt = DateTime.SpecifyKind(default, DateTimeKind.Utc);

        Assert.Throws<DomainValidationException>(() => CreateReading(recordedAt: recordedAt));
    }

    [Fact]
    public void Should_StoreRecordedAtAsUtc_When_RecordedAtIsLocal()
    {
        var recordedAtLocal = FiveMinutesAgo.ToLocalTime();

        var vitalReading = CreateReading(recordedAt: recordedAtLocal);

        Assert.Equal(DateTimeKind.Utc, vitalReading.RecordedAtUtc.Kind);
        Assert.Equal(FiveMinutesAgo, vitalReading.RecordedAtUtc);
    }

    [Fact]
    public void Should_ThrowDomainValidationException_When_RecordedAtIsLocalAndInTheFuture()
    {
        var recordedAtLocal = Now.UtcDateTime.AddHours(1).ToLocalTime();

        Assert.Throws<DomainValidationException>(() => CreateReading(recordedAt: recordedAtLocal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_ThrowDomainValidationException_When_DeviceIdIsBlank(string deviceId)
    {
        Assert.Throws<DomainValidationException>(() => CreateReading(deviceId: deviceId));
    }

    [Fact]
    public void Should_TrimDeviceId_When_DeviceIdHasSurroundingSpaces()
    {
        var vitalReading = CreateReading(deviceId: "  monitor-01  ");

        Assert.Equal("monitor-01", vitalReading.DeviceId);
    }

    [Fact]
    public void Should_ThrowDomainValidationException_When_DeviceIdExceedsMaxLength()
    {
        var deviceId = new string('x', VitalReading.DeviceIdMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => CreateReading(deviceId: deviceId));
    }

    [Fact]
    public void Should_CreateVitalReading_When_DeviceIdHasMaxLengthAfterTrim()
    {
        var deviceId = "  " + new string('x', VitalReading.DeviceIdMaxLength) + "  ";

        var vitalReading = CreateReading(deviceId: deviceId);

        Assert.Equal(VitalReading.DeviceIdMaxLength, vitalReading.DeviceId.Length);
    }
}
