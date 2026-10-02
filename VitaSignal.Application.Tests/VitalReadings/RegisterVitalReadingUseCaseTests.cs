using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using VitaSignal.Application.Alerts;
using VitaSignal.Application.Common;
using VitaSignal.Application.Common.Exceptions;
using VitaSignal.Application.Patients;
using VitaSignal.Application.Patients.Exceptions;
using VitaSignal.Application.Tests.Support;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.Alerts.Enums;
using VitaSignal.Domain.Common;
using VitaSignal.Domain.VitalReadings;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Application.Tests.VitalReadings;

public sealed class RegisterVitalReadingUseCaseTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IPatientRepository> _patientRepositoryMock = new();
    private readonly Mock<IVitalReadingRepository> _vitalReadingRepositoryMock = new();
    private readonly Mock<IVitalAlertRepository> _vitalAlertRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly TestMeterFactory _meterFactory = new();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly RegisterVitalReadingUseCase _useCase;
    private readonly Guid _patientId = Guid.NewGuid();

    public RegisterVitalReadingUseCaseTests()
    {
        _useCase = new RegisterVitalReadingUseCase(
            _patientRepositoryMock.Object,
            _vitalReadingRepositoryMock.Object,
            _vitalAlertRepositoryMock.Object,
            _unitOfWorkMock.Object,
            new VitalReadingMetrics(_meterFactory),
            _timeProvider,
            NullLogger<RegisterVitalReadingUseCase>.Instance);

        _patientRepositoryMock
            .Setup(r => r.ExistsAsync(_patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    public void Dispose() => _meterFactory.Dispose();

    private RegisterVitalReadingRequest Request(
        VitalSignType? type = VitalSignType.HeartRate,
        double value = 75,
        DateTime? recordedAtUtc = null,
        Guid? patientId = null) =>
        new(patientId ?? _patientId, type, value, recordedAtUtc ?? Now.UtcDateTime.AddMinutes(-1), "device-test");

    private MetricCollector<long> Collect(string instrumentName) =>
        new(_meterFactory, VitalReadingMetrics.MeterName, instrumentName);

    [Fact]
    public async Task Should_RegisterVitalReading_When_PatientExists()
    {
        var result = await _useCase.ExecuteAsync(Request(), CancellationToken.None);

        Assert.Equal(_patientId, result.Reading.PatientId);
        Assert.True(result.IsWithinNormalRange);
        Assert.Null(result.Alert);
        _vitalReadingRepositoryMock.Verify(r => r.AddAsync(result.Reading, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_LookUpTheRequestedPatient_When_Executed()
    {
        await _useCase.ExecuteAsync(Request(), CancellationToken.None);

        _patientRepositoryMock.Verify(r => r.ExistsAsync(_patientId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ThrowPatientNotFoundException_When_PatientDoesNotExist()
    {
        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => _useCase.ExecuteAsync(Request(patientId: Guid.NewGuid()), CancellationToken.None));

        _vitalReadingRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<VitalReading>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_RaiseCriticalAlert_When_ValueIsOutsideCriticalRange()
    {
        var result = await _useCase.ExecuteAsync(Request(value: 180), CancellationToken.None);

        Assert.False(result.IsWithinNormalRange);
        Assert.NotNull(result.Alert);
        Assert.Equal(AlertSeverity.Critical, result.Alert.Severity);
        _vitalAlertRepositoryMock.Verify(r => r.AddAsync(result.Alert, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_NotPersistAlert_When_ValueIsWithinNormalRange()
    {
        await _useCase.ExecuteAsync(Request(value: 75), CancellationToken.None);

        _vitalAlertRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<VitalAlert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_UseTimeProvider_When_ValidatingFutureReadings()
    {
        var request = Request(recordedAtUtc: Now.UtcDateTime.AddMinutes(5));

        await Assert.ThrowsAsync<DomainValidationException>(() => _useCase.ExecuteAsync(request, CancellationToken.None));

        _timeProvider.Advance(TimeSpan.FromMinutes(10));
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(request.RecordedAtUtc, result.Reading.RecordedAtUtc);
    }

    [Fact]
    public async Task Should_ThrowInvalidRequestException_When_TypeIsMissing()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _useCase.ExecuteAsync(Request(type: null), CancellationToken.None));
    }

    [Fact]
    public async Task Should_ThrowInvalidRequestException_When_RecordedAtIsMissing()
    {
        var request = Request() with { RecordedAtUtc = null };

        await Assert.ThrowsAsync<InvalidRequestException>(() => _useCase.ExecuteAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Should_NotQueryRepositories_When_RequiredFieldIsMissing()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _useCase.ExecuteAsync(Request(type: null), CancellationToken.None));

        _patientRepositoryMock.Verify(
            r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_NotSaveChanges_When_ValueIsNotPlausible()
    {
        await Assert.ThrowsAsync<ImplausibleVitalValueException>(
            () => _useCase.ExecuteAsync(Request(type: VitalSignType.SpO2, value: 250), CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_RecordRegisteredMetric_When_ReadingIsRegistered()
    {
        using var registered = Collect("vitasignal.vital_readings.registered");
        using var outOfRange = Collect("vitasignal.vital_readings.out_of_range");

        await _useCase.ExecuteAsync(Request(value: 75), CancellationToken.None);

        var measurement = Assert.Single(registered.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("HeartRate", measurement.Tags["vital_sign_type"]);
        Assert.Empty(outOfRange.GetMeasurementSnapshot());
    }

    [Fact]
    public async Task Should_RecordOutOfRangeAndAlertMetrics_When_ReadingIsOutsideNormalRange()
    {
        using var outOfRange = Collect("vitasignal.vital_readings.out_of_range");
        using var alertsRaised = Collect("vitasignal.alerts.raised");

        await _useCase.ExecuteAsync(Request(value: 110), CancellationToken.None);

        Assert.Single(outOfRange.GetMeasurementSnapshot());
        var alertMeasurement = Assert.Single(alertsRaised.GetMeasurementSnapshot());
        Assert.Equal("Warning", alertMeasurement.Tags["severity"]);
    }

    [Theory]
    [InlineData(RejectionReasons.PatientNotFound)]
    [InlineData(RejectionReasons.ImplausibleValue)]
    [InlineData(RejectionReasons.ValidationFailed)]
    public async Task Should_RecordRejectedMetricWithReason_When_ReadingIsRejected(string expectedReason)
    {
        using var rejected = Collect("vitasignal.vital_readings.rejected");
        using var registered = Collect("vitasignal.vital_readings.registered");

        var request = expectedReason switch
        {
            RejectionReasons.PatientNotFound => Request(patientId: Guid.NewGuid()),
            RejectionReasons.ImplausibleValue => Request(type: VitalSignType.SpO2, value: 250),
            _ => Request(type: null)
        };

        await Assert.ThrowsAnyAsync<Exception>(() => _useCase.ExecuteAsync(request, CancellationToken.None));

        var measurement = Assert.Single(rejected.GetMeasurementSnapshot());
        Assert.Equal(expectedReason, measurement.Tags["reason"]);
        Assert.Empty(registered.GetMeasurementSnapshot());
    }
}
