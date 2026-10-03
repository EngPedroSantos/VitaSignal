using VitaSignal.Domain.Alerts.Enums;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Api.Contracts;

public sealed record PatientResponse(Guid Id, string Code, bool IsSyntheticData);

public sealed record VitalReadingResponse(
    Guid Id,
    Guid PatientId,
    VitalSignType Type,
    double Value,
    string Unit,
    DateTime RecordedAtUtc,
    string DeviceId);

public sealed record VitalAlertResponse(
    Guid Id,
    Guid PatientId,
    Guid ReadingId,
    VitalSignType Type,
    double Value,
    AlertSeverity Severity,
    DateTime RaisedAtUtc,
    DateTime? AcknowledgedAtUtc);

public sealed record RegisterVitalReadingResponse(
    VitalReadingResponse Reading,
    bool IsWithinNormalRange,
    VitalAlertResponse? Alert);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, string? NextCursor);
