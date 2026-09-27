using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Application.VitalReadings;

public sealed record RegisterVitalReadingResult(VitalReading Reading, bool IsWithinNormalRange);