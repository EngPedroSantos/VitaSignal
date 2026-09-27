using VitaSignal.Domain.Patients;

namespace VitaSignal.Domain.Tests.Patients
{
    public class PatientTests
    {
        [Fact]
        public void Should_CreatePatient_When_DisplayNameIsValid()
        {
            var displayName = "Pedro Henrique";
            
            var patient = Patient.Create(displayName);

            Assert.Equal(displayName, patient.DisplayName);
            Assert.NotEqual(Guid.Empty, patient.Id);
        }

        [Fact]
        public void Should_ThrowArgumentException_When_DisplayNameIsEmpty()
        {
            string displayName = string.Empty;

            Assert.Throws<ArgumentException>(() => Patient.Create(displayName));
        }
    }
}
