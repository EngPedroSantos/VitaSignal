using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.Tests.VitalReadings
{
    public class VitalReadingTests
    {
        [Theory]
        [InlineData(VitalSignType.HeartRate, "bpm")]
        [InlineData(VitalSignType.SpO2, "%")]
        [InlineData(VitalSignType.BodyTemperature, "°C")]
        [InlineData(VitalSignType.SystolicBloodPressure, "mmHg")]
        [InlineData(VitalSignType.DiastolicBloodPressure, "mmHg")]
        public void Should_CreateVitalReading_When_DataIsValid(VitalSignType type, string expectedUnit)
        {
            var vitalReading = VitalReading.Create(Guid.NewGuid(), type, 70, DateTime.UtcNow, "device123");

            Assert.Equal(expectedUnit, vitalReading.Unit);
            Assert.Equal(type, vitalReading.Type);
        }

        [Fact]
        public void Should_ThrowArgumentException_When_PatientIdIsEmpty()
        {
            var patientId = Guid.Empty;

            Assert.Throws<ArgumentException>(() => VitalReading.Create(patientId, VitalSignType.HeartRate, 70, DateTime.UtcNow, "device123"));
        }

        [Fact]
        public void Should_ThrowArgumentOutOfRangeException_When_VitalSign_ValueIsNegative()
        {
            var vitalSign = -1;

            Assert.Throws<ArgumentOutOfRangeException>(() => VitalReading.Create(Guid.NewGuid(), VitalSignType.HeartRate, vitalSign, DateTime.UtcNow, "device123"));
        }

        [Fact]
        public void Should_ThrowArgumentException_When_RecordedAtIsInTheFuture()
        {
            var recordedAtUtc = DateTime.UtcNow.AddHours(1);

            Assert.Throws<ArgumentException>(() => VitalReading.Create(Guid.NewGuid(), VitalSignType.BodyTemperature, 70, recordedAtUtc, "device123"));
        }

        [Fact]
        public void Should_ThrowArgumentException_When_DeviceIdIsEmpty()
        {
            var deviceId = string.Empty;

            Assert.Throws<ArgumentException>(() => VitalReading.Create(Guid.NewGuid(), VitalSignType.SystolicBloodPressure, 70, DateTime.UtcNow, deviceId));
        }

        [Theory]
        [InlineData(50, true)]    // dentro da faixa
        [InlineData(9, false)]    // abaixo do Min
        [InlineData(101, false)]  // acima do Max
        [InlineData(10, true)]    // exatamente no Min — limite inclusivo
        [InlineData(100, true)]   // exatamente no Max — limite inclusivo
        public void Should_EvaluateContainsCorrectly_When_GivenDifferentValues(double value, bool expected)
        {
            var range = new VitalRange(10, 100);

            var result = range.Contains(value);

            Assert.Equal(expected, result);
        }

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

        [Fact]
        public void Should_ThrowArgumentOutOfRangeException_When_TypeIsUnknown()
        {
            var invalidType = (VitalSignType)999;

            Assert.Throws<ArgumentOutOfRangeException>(() => VitalRangeCatalog.GetNormalRange(invalidType));
        }
    }
}