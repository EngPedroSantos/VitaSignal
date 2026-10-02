using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VitaSignal.Application.Common;
using VitaSignal.Application.Patients;
using VitaSignal.Domain.Patients;

namespace VitaSignal.Application.Tests.Patients;

public class RegisterPatientUseCaseTests
{
    private readonly Mock<IPatientRepository> _patientRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RegisterPatientUseCase _useCase;

    public RegisterPatientUseCaseTests()
    {
        _useCase = new RegisterPatientUseCase(
            _patientRepositoryMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<RegisterPatientUseCase>.Instance);
    }

    [Fact]
    public async Task Should_RegisterPatientWithPseudonymousCode_When_Executed()
    {
        var patient = await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.StartsWith(Patient.CodePrefix, patient.Code, StringComparison.Ordinal);
        _patientRepositoryMock.Verify(r => r.AddAsync(patient, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_SaveChanges_When_PatientIsRegistered()
    {
        await _useCase.ExecuteAsync(CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
