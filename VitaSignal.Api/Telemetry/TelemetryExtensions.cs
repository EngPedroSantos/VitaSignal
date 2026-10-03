using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using VitaSignal.Application.Common.Diagnostics;
using VitaSignal.Application.VitalReadings;

namespace VitaSignal.Api.Telemetry;

internal static class TelemetryExtensions
{
    private static readonly string[] UntracedPaths = ["/metrics", "/health/live", "/health/ready"];

    public static WebApplicationBuilder AddVitaSignalTelemetry(this WebApplicationBuilder builder)
    {
        var useConsoleExporter = builder.Configuration.GetValue("Telemetry:ConsoleExporter", false);
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (!builder.Environment.IsDevelopment())
            builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;

            if (useOtlpExporter)
                logging.AddOtlpExporter();
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("VitaSignal.Api"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context => !UntracedPaths.Contains(context.Request.Path.Value, StringComparer.OrdinalIgnoreCase))
                    .AddHttpClientInstrumentation()
                    .AddSource("Npgsql")
                    .AddSource(ApplicationDiagnostics.SourceName);

                if (useConsoleExporter)
                    tracing.AddConsoleExporter();

                if (useOtlpExporter)
                    tracing.AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddMeter(VitalReadingMetrics.MeterName)
                    .AddPrometheusExporter();

                if (useConsoleExporter)
                {
                    metrics.AddConsoleExporter((_, readerOptions) =>
                        readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000);
                }

                if (useOtlpExporter)
                    metrics.AddOtlpExporter();
            });

        return builder;
    }
}
