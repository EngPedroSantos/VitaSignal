using Moq;
using VitaSignal.Application.Alerts;
using VitaSignal.Application.Common.Pagination;
using VitaSignal.Application.Patients;
using VitaSignal.Application.Patients.Exceptions;
using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.Alerts.Enums;

namespace VitaSignal.Application.Tests.Alerts;

public class ListPatientAlertsUseCaseTests
{
    private readonly Mock<IPatientRepository> _patientRepositoryMock = new();
    private readonly Mock<IVitalAlertRepository> _vitalAlertRepositoryMock = new();
    private readonly ListPatientAlertsUseCase _useCase;
    private readonly Guid _patientId = Guid.NewGuid();

    public ListPatientAlertsUseCaseTests()
    {
        _useCase = new ListPatientAlertsUseCase(_patientRepositoryMock.Object, _vitalAlertRepositoryMock.Object);

        _patientRepositoryMock
            .Setup(r => r.ExistsAsync(_patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _vitalAlertRepositoryMock
            .Setup(r => r.GetPageByPatientIdAsync(
                It.IsAny<Guid>(), It.IsAny<AlertSeverity?>(), It.IsAny<bool>(), It.IsAny<KeysetCursor?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VitalAlert>());
    }

    [Fact]
    public async Task Should_ForwardFilters_When_Listing()
    {
        await _useCase.ExecuteAsync(_patientId, AlertSeverity.Critical, pendingOnly: true, null, 20, CancellationToken.None);

        _vitalAlertRepositoryMock.Verify(r => r.GetPageByPatientIdAsync(
            _patientId, AlertSeverity.Critical, true, null, 21, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ThrowPatientNotFoundException_When_PatientDoesNotExist()
    {
        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => _useCase.ExecuteAsync(Guid.NewGuid(), null, false, null, null, CancellationToken.None));
    }
}
