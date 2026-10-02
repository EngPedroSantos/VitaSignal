using VitaSignal.Domain.Patients;

namespace VitaSignal.Domain.Tests.Patients;

public class PatientTests
{
    [Fact]
    public void Should_GeneratePseudonymousCode_When_PatientIsCreated()
    {
        var patient = Patient.Create();

        Assert.NotEqual(Guid.Empty, patient.Id);
        Assert.StartsWith(Patient.CodePrefix, patient.Code, StringComparison.Ordinal);
        Assert.Equal(Patient.CodeLength, patient.Code.Length);
    }

    [Fact]
    public void Should_DeriveCodeFromId_When_PatientIsCreated()
    {
        var patient = Patient.Create();

        var expectedSuffix = patient.Id.ToString("N")[..12].ToUpperInvariant();

        Assert.Equal(Patient.CodePrefix + expectedSuffix, patient.Code);
    }

    [Fact]
    public void Should_GenerateDifferentCodes_When_TwoPatientsAreCreated()
    {
        var first = Patient.Create();
        var second = Patient.Create();

        Assert.NotEqual(first.Code, second.Code);
    }

    [Fact]
    public void Should_FlagDataAsSynthetic_Always()
    {
        Assert.True(Patient.IsSyntheticData);
    }
}
