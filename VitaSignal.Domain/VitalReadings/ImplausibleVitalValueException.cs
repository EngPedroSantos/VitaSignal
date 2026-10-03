using VitaSignal.Domain.Common;
using VitaSignal.Domain.VitalReadings.Enums;

namespace VitaSignal.Domain.VitalReadings;

public sealed class ImplausibleVitalValueException : DomainValidationException
{
    public VitalSignType Type { get; }
    public double Value { get; }

    public ImplausibleVitalValueException(VitalSignType type, double value, VitalRange plausibleRange)
        : base($"{type} value {value} is outside what a device can measure ({plausibleRange.Min}-{plausibleRange.Max}).")
    {
        Type = type;
        Value = value;
    }
}
