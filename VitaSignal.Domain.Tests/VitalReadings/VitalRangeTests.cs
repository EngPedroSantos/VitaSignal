using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Domain.Tests.VitalReadings;

public class VitalRangeTests
{
    [Theory]
    [InlineData(50, true)]
    [InlineData(9, false)]
    [InlineData(101, false)]
    [InlineData(10, true)]
    [InlineData(100, true)]
    public void Should_EvaluateContainsCorrectly_When_GivenDifferentValues(double value, bool expected)
    {
        var range = new VitalRange(10, 100);

        var result = range.Contains(value);

        Assert.Equal(expected, result);
    }
}
