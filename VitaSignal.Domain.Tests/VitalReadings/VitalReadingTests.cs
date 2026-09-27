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
    }
}