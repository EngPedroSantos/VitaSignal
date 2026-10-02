using Moq;
using VitaSignal.Application.Common.Exceptions;
using VitaSignal.Application.Common.Pagination;
using VitaSignal.Application.Patients;
using VitaSignal.Application.Patients.Exceptions;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Application.Tests.VitalReadings;

public class ListPatientReadingsUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IPatientRepository> _patientRepositoryMock = new();
    private readonly Mock<IVitalReadingRepository> _vitalReadingRepositoryMock = new();
    private readonly ListPatientReadingsUseCase _useCase;
    private readonly Guid _patientId = Guid.NewGuid();

    public ListPatientReadingsUseCaseTests()
    {
        _useCase = new ListPatientReadingsUseCase(_patientRepositoryMock.Object, _vitalReadingRepositoryMock.Object);

        _patientRepositoryMock
            .Setup(r => r.ExistsAsync(_patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private List<VitalReading> Readings(int count) =>
        Enumerable.Range(1, count)
            .Select(i => VitalReading.Create(_patientId, VitalSignType.HeartRate, 70, Now.UtcDateTime.AddMinutes(-i), "device", Now))
            .ToList();

    private void SetupRows(List<VitalReading> rows) =>
        _vitalReadingRepositoryMock
            .Setup(r => r.GetPageByPatientIdAsync(
                _patientId, It.IsAny<VitalSignType?>(), It.IsAny<KeysetCursor?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

    [Fact]
    public async Task Should_AskRepositoryForOneExtraRow_When_LimitIsGiven()
    {
        SetupRows(Readings(3));

        await _useCase.ExecuteAsync(_patientId, null, null, 10, CancellationToken.None);

        _vitalReadingRepositoryMock.Verify(r => r.GetPageByPatientIdAsync(
            _patientId, null, null, 11, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnNextCursor_When_ThereAreMoreRowsThanLimit()
    {
        var rows = Readings(3);
        SetupRows(rows);

        var page = await _useCase.ExecuteAsync(_patientId, null, null, 2, CancellationToken.None);

        Assert.Equal(2, page.Items.Count);
        Assert.NotNull(page.NextCursor);
        Assert.Equal(new KeysetCursor(rows[1].RecordedAtUtc, rows[1].Id), KeysetCursor.Decode(page.NextCursor));
    }

    [Fact]
    public async Task Should_NotReturnNextCursor_When_LastPageIsReached()
    {
        SetupRows(Readings(2));

        var page = await _useCase.ExecuteAsync(_patientId, null, null, 2, CancellationToken.None);

        Assert.Equal(2, page.Items.Count);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task Should_UseDefaultLimit_When_LimitIsNotGiven()
    {
        SetupRows([]);

        await _useCase.ExecuteAsync(_patientId, null, null, null, CancellationToken.None);

        _vitalReadingRepositoryMock.Verify(r => r.GetPageByPatientIdAsync(
            _patientId, null, null, Paging.DefaultLimit + 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(Paging.MaxLimit + 1)]
    public async Task Should_ThrowInvalidRequestException_When_LimitIsOutOfBounds(int limit)
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _useCase.ExecuteAsync(_patientId, null, null, limit, CancellationToken.None));
    }

    [Fact]
    public async Task Should_ThrowPatientNotFoundException_When_PatientDoesNotExist()
    {
        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => _useCase.ExecuteAsync(Guid.NewGuid(), null, null, null, CancellationToken.None));
    }
}
