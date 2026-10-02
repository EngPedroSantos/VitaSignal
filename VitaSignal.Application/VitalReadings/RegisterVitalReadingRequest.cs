using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Application.VitalReadings;

public sealed record RegisterVitalReadingRequest(
    Guid PatientId,
    VitalSignType? Type,
    double Value,
    DateTime? RecordedAtUtc,
    string DeviceId);
