using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.Tests.VitalReadings;

public class VitalPlausibilityCatalogTests
{
    [Theory]
    [InlineData(VitalSignType.HeartRate)]
    [InlineData(VitalSignType.SpO2)]
    [InlineData(VitalSignType.BodyTemperature)]
    [InlineData(VitalSignType.SystolicBloodPressure)]
    [InlineData(VitalSignType.DiastolicBloodPressure)]
    public void Should_ContainCriticalRange_When_TypeIsKnown(VitalSignType type)
    {
        var plausibleRange = VitalPlausibilityCatalog.GetPlausibleRange(type);
        var criticalRange = VitalRangeCatalog.GetCriticalRange(type);

        Assert.True(plausibleRange.Contains(criticalRange.Min));
        Assert.True(plausibleRange.Contains(criticalRange.Max));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_TypeIsUnknown()
    {
        var invalidType = (VitalSignType)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => VitalPlausibilityCatalog.GetPlausibleRange(invalidType));
    }
}
