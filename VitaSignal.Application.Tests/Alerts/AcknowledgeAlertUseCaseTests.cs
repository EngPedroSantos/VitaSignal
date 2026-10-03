using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using VitaSignal.Application.Alerts;
using VitaSignal.Application.Alerts.Exceptions;
using VitaSignal.Application.Common;
using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Application.Tests.Alerts;

public class AcknowledgeAlertUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IVitalAlertRepository> _vitalAlertRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly AcknowledgeAlertUseCase _useCase;

    public AcknowledgeAlertUseCaseTests()
    {
        _useCase = new AcknowledgeAlertUseCase(
            _vitalAlertRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _timeProvider,
            NullLogger<AcknowledgeAlertUseCase>.Instance);
    }

    private VitalAlert SetupAlert()
    {
        var reading = VitalReading.Create(Guid.NewGuid(), VitalSignType.HeartRate, 180, Now.UtcDateTime.AddMinutes(-1), "device", Now);
        var alert = VitalAlert.RaiseIfNeeded(reading, Now)!;

        _vitalAlertRepositoryMock
            .Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        return alert;
    }

    [Fact]
    public async Task Should_AcknowledgeAndSave_When_AlertIsPending()
    {
        var alert = SetupAlert();
        _timeProvider.Advance(TimeSpan.FromMinutes(4));

        var result = await _useCase.ExecuteAsync(alert.Id, CancellationToken.None);

        Assert.True(result.IsAcknowledged);
        Assert.Equal(Now.UtcDateTime.AddMinutes(4), result.AcknowledgedAtUtc);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_NotSaveAgain_When_AlertIsAlreadyAcknowledged()
    {
        var alert = SetupAlert();
        alert.Acknowledge(Now);

        await _useCase.ExecuteAsync(alert.Id, CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_ThrowAlertNotFoundException_When_AlertDoesNotExist()
    {
        await Assert.ThrowsAsync<AlertNotFoundException>(
            () => _useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
