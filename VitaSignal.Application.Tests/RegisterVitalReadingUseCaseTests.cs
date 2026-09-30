using Moq;
using VitaSignal.Application.Patients;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Domain.Patients;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Application.Tests.VitalReadings;

public class RegisterVitalReadingUseCaseTests
{
    private readonly Mock<IPatientRepository> _patientRepositoryMock;
    private readonly Mock<IVitalReadingRepository> _vitalReadingRepositoryMock;
    private readonly RegisterVitalReadingUseCase _useCase;

    public RegisterVitalReadingUseCaseTests()
    {
        _patientRepositoryMock = new Mock<IPatientRepository>();
        _vitalReadingRepositoryMock = new Mock<IVitalReadingRepository>();
        _useCase = new RegisterVitalReadingUseCase(
            _patientRepositoryMock.Object,
            _vitalReadingRepositoryMock.Object);
    }

    [Fact]
    public async Task Should_ThrowPatientNotFoundException_When_PatientDoesNotExist()
    {
        _patientRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        var request = new RegisterVitalReadingRequest(
            PatientId: Guid.NewGuid(),
            Type: VitalSignType.BodyTemperature,
            Value: 36.5,
            RecordedAtUtc: DateTime.UtcNow,
            DeviceId: "device-teste"
        );

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => _useCase.ExecuteAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Should_NotCallAddAsync_When_PatientDoesNotExist()
    {
        _patientRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        var request = new RegisterVitalReadingRequest(
            PatientId: Guid.NewGuid(),
            Type: VitalSignType.BodyTemperature,
            Value: 36.5,
            RecordedAtUtc: DateTime.UtcNow,
            DeviceId: "device-teste"
        );

        try { await _useCase.ExecuteAsync(request, CancellationToken.None); }
        catch (PatientNotFoundException) {  }

        _vitalReadingRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Domain.VitalReadings.VitalReading>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_RegisterVitalReading_When_PatientExists()
    {
        var patient = Patient.Create("Test Name");

        var request = new RegisterVitalReadingRequest(
            PatientId: patient.Id,
            Type: VitalSignType.BodyTemperature,
            Value: 36.5,
            RecordedAtUtc: DateTime.UtcNow,
            DeviceId: "device-test"
        );

        _patientRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(patient.Id, result.Reading.PatientId);
        Assert.True(result.IsWithinNormalRange);

        _vitalReadingRepositoryMock.Verify(
            r => r.AddAsync(result.Reading, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_HaveIsWithinNormalRangeFalse_When_ValueIsOutsideNormalRange()
    {
        var patient = Patient.Create("Test Name");

        var request = new RegisterVitalReadingRequest(
            PatientId: patient.Id,
            Type: VitalSignType.HeartRate,
            Value: 180,
            RecordedAtUtc: DateTime.UtcNow,
            DeviceId: "device-test"
        );

        _patientRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsWithinNormalRange);
    }
}