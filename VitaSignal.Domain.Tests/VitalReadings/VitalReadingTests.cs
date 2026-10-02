using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.Tests.VitalReadings
{
    public class VitalReadingTests
    {
        [Theory]
        [InlineData(VitalSignType.HeartRate, 70, "bpm")]
        [InlineData(VitalSignType.SpO2, 98, "%")]
        [InlineData(VitalSignType.BodyTemperature, 36.5, "°C")]
        [InlineData(VitalSignType.SystolicBloodPressure, 110, "mmHg")]
        [InlineData(VitalSignType.DiastolicBloodPressure, 70, "mmHg")]
        public void Should_CreateVitalReading_When_DataIsValid(VitalSignType type, double value, string expectedUnit)
        {
            var vitalReading = VitalReading.Create(Guid.NewGuid(), type, value, DateTime.UtcNow, "device123");

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

            Assert.Throws<ArgumentException>(() => VitalReading.Create(Guid.NewGuid(), VitalSignType.BodyTemperature, 36.5, recordedAtUtc, "device123"));
        }

        [Fact]
        public void Should_ThrowArgumentException_When_DeviceIdIsEmpty()
        {
            var deviceId = string.Empty;

            Assert.Throws<ArgumentException>(() => VitalReading.Create(Guid.NewGuid(), VitalSignType.SystolicBloodPressure, 70, DateTime.UtcNow, deviceId));
        }

        [Fact]
        public void Should_ThrowArgumentException_When_RecordedAtHasNoTimezone()
        {
            var recordedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Unspecified);

            Assert.Throws<ArgumentException>(() => VitalReading.Create(Guid.NewGuid(), VitalSignType.HeartRate, 70, recordedAt, "device123"));
        }

        [Fact]
        public void Should_ThrowArgumentException_When_RecordedAtIsDefault()
        {
            var recordedAt = DateTime.SpecifyKind(default, DateTimeKind.Utc);

            Assert.Throws<ArgumentException>(() => VitalReading.Create(Guid.NewGuid(), VitalSignType.HeartRate, 70, recordedAt, "device123"));
        }

        [Fact]
        public void Should_StoreRecordedAtAsUtc_When_RecordedAtIsLocal()
        {
            var recordedAtUtc = DateTime.UtcNow.AddMinutes(-5);
            var recordedAtLocal = recordedAtUtc.ToLocalTime();

            var vitalReading = VitalReading.Create(Guid.NewGuid(), VitalSignType.HeartRate, 70, recordedAtLocal, "device123");

            Assert.Equal(DateTimeKind.Utc, vitalReading.RecordedAtUtc.Kind);
            Assert.Equal(recordedAtUtc, vitalReading.RecordedAtUtc);
        }

        [Fact]
        public void Should_ThrowArgumentException_When_RecordedAtIsLocalAndInTheFuture()
        {
            var recordedAtLocal = DateTime.UtcNow.AddHours(1).ToLocalTime();

            Assert.Throws<ArgumentException>(() => VitalReading.Create(Guid.NewGuid(), VitalSignType.HeartRate, 70, recordedAtLocal, "device123"));
        }

        [Fact]
        public void Should_ThrowArgumentOutOfRangeException_When_TypeIsNotDefined()
        {
            var invalidType = (VitalSignType)999;

            Assert.Throws<ArgumentOutOfRangeException>(() => VitalReading.Create(Guid.NewGuid(), invalidType, 70, DateTime.UtcNow, "device123"));
        }

        [Theory]
        [InlineData(VitalSignType.SpO2, 250)]
        [InlineData(VitalSignType.SpO2, 0)]
        [InlineData(VitalSignType.HeartRate, 400)]
        [InlineData(VitalSignType.HeartRate, 10)]
        [InlineData(VitalSignType.BodyTemperature, 70)]
        [InlineData(VitalSignType.BodyTemperature, 20)]
        [InlineData(VitalSignType.SystolicBloodPressure, 350)]
        [InlineData(VitalSignType.DiastolicBloodPressure, 10)]
        public void Should_ThrowArgumentOutOfRangeException_When_ValueIsNotPlausible(VitalSignType type, double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VitalReading.Create(Guid.NewGuid(), type, value, DateTime.UtcNow, "device123"));
        }

        [Theory]
        [InlineData(VitalSignType.SpO2, 100)]
        [InlineData(VitalSignType.HeartRate, 20)]
        [InlineData(VitalSignType.HeartRate, 300)]
        [InlineData(VitalSignType.BodyTemperature, 45)]
        public void Should_CreateVitalReading_When_ValueIsAtPlausibleLimit(VitalSignType type, double value)
        {
            var vitalReading = VitalReading.Create(Guid.NewGuid(), type, value, DateTime.UtcNow, "device123");

            Assert.Equal(value, vitalReading.Value);
        }

        [Fact]
        public void Should_CreateVitalReading_When_ValueIsOutsideNormalRangeButPlausible()
        {
            var vitalReading = VitalReading.Create(Guid.NewGuid(), VitalSignType.HeartRate, 180, DateTime.UtcNow, "device123");

            Assert.Equal(180, vitalReading.Value);
        }

        [Fact]
        public void Should_ThrowArgumentException_When_DeviceIdExceedsMaxLength()
        {
            var deviceId = new string('x', VitalReading.DeviceIdMaxLength + 1);

            Assert.Throws<ArgumentException>(() => VitalReading.Create(Guid.NewGuid(), VitalSignType.HeartRate, 70, DateTime.UtcNow, deviceId));
        }

        [Fact]
        public void Should_CreateVitalReading_When_DeviceIdHasMaxLengthAfterTrim()
        {
            var deviceId = "  " + new string('x', VitalReading.DeviceIdMaxLength) + "  ";

            var vitalReading = VitalReading.Create(Guid.NewGuid(), VitalSignType.HeartRate, 70, DateTime.UtcNow, deviceId);

            Assert.Equal(VitalReading.DeviceIdMaxLength, vitalReading.DeviceId.Length);
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