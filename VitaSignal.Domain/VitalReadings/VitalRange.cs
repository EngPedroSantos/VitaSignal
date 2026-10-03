namespace VitaSignal.Domain.VitalReadings;

public readonly record struct VitalRange(double Min, double Max)
{
    public bool Contains(double value) => value >= Min && value <= Max;
}
