using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using VitaSignal.DeviceSimulator;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<SimulatorOptions>()
    .Bind(builder.Configuration.GetSection(SimulatorOptions.SectionName))
    .Validate(options => options.IsValid(), "Simulator settings are out of range.")
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHttpClient<VitaSignalApiClient>((services, client) =>
    {
        client.BaseAddress = services.GetRequiredService<IOptions<SimulatorOptions>>().Value.ApiBaseUrl;
    })
    .AddStandardResilienceHandler();

builder.Services.AddHostedService<DeviceSimulatorWorker>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("VitaSignal.DeviceSimulator"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(DeviceSimulatorWorker.ActivitySourceName)
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            tracing.AddOtlpExporter();
    });

await builder.Build().RunAsync();
