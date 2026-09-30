using Moq;
using VitaSignal.Application.Patients;
using VitaSignal.Domain.Patients;

namespace VitaSignal.Application.Tests.Patients;

public class RegisterPatientUseCaseTests
{
    private readonly Mock<IPatientRepository> _patientRepositoryMock;
    private readonly RegisterPatientUseCase _useCase;

    public RegisterPatientUseCaseTests()
    {
        _patientRepositoryMock = new Mock<IPatientRepository>();
        _useCase = new RegisterPatientUseCase(_patientRepositoryMock.Object);
    }

    [Fact]
    public async Task Should_RegisterPatient_When_DisplayNameIsValid()
    {
        var request = new RegisterPatientRequest("Paciente Teste");

        var patient = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal("Paciente Teste", patient.DisplayName);
        _patientRepositoryMock.Verify(
            r => r.AddAsync(patient, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_DisplayNameIsEmpty()
    {
        var request = new RegisterPatientRequest("");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _useCase.ExecuteAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Should_NotCallAddAsync_When_DisplayNameIsEmpty()
    {
        var request = new RegisterPatientRequest("");

        try { await _useCase.ExecuteAsync(request, CancellationToken.None); }
        catch (ArgumentException) { }

        _patientRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}