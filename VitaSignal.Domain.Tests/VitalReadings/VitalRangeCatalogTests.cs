using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.Tests.VitalReadings;

public class VitalRangeCatalogTests
{
    [Theory]
    [InlineData(VitalSignType.HeartRate, 60, 100)]
    [InlineData(VitalSignType.SpO2, 95, 100)]
    [InlineData(VitalSignType.BodyTemperature, 36.1, 37.2)]
    [InlineData(VitalSignType.SystolicBloodPressure, 90, 120)]
    [InlineData(VitalSignType.DiastolicBloodPressure, 60, 80)]
    public void Should_ReturnExpectedRange_When_TypeIsKnown(VitalSignType type, double expectedMin, double expectedMax)
    {
        var range = VitalRangeCatalog.GetNormalRange(type);

        Assert.Equal(expectedMin, range.Min);
        Assert.Equal(expectedMax, range.Max);
    }

    [Theory]
    [InlineData(VitalSignType.HeartRate)]
    [InlineData(VitalSignType.SpO2)]
    [InlineData(VitalSignType.BodyTemperature)]
    [InlineData(VitalSignType.SystolicBloodPressure)]
    [InlineData(VitalSignType.DiastolicBloodPressure)]
    public void Should_ContainNormalRangeInsideCriticalRange_When_TypeIsKnown(VitalSignType type)
    {
        var criticalRange = VitalRangeCatalog.GetCriticalRange(type);
        var normalRange = VitalRangeCatalog.GetNormalRange(type);

        Assert.True(criticalRange.Contains(normalRange.Min));
        Assert.True(criticalRange.Contains(normalRange.Max));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_TypeIsUnknown()
    {
        var invalidType = (VitalSignType)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => VitalRangeCatalog.GetNormalRange(invalidType));
        Assert.Throws<ArgumentOutOfRangeException>(() => VitalRangeCatalog.GetCriticalRange(invalidType));
    }
}
