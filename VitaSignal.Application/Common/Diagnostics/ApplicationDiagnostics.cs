using System.Diagnostics;

namespace VitaSignal.Application.Common.Diagnostics;

public static class ApplicationDiagnostics
{
    public const string SourceName = "VitaSignal.Application";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
