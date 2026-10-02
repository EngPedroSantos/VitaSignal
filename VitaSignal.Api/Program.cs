using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using VitaSignal.Api;
using VitaSignal.Api.Telemetry;
using VitaSignal.Application.Alerts;
using VitaSignal.Application.Common;
using VitaSignal.Application.Patients;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Infrastructure.Alerts;
using VitaSignal.Infrastructure.Patients;
using VitaSignal.Infrastructure.Persistence;
using VitaSignal.Infrastructure.VitalReadings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

builder.Services.AddDbContext<VitaSignalDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("VitaSignalDb"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<VitalReadingMetrics>();

builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IVitalReadingRepository, VitalReadingRepository>();
builder.Services.AddScoped<IVitalAlertRepository, VitalAlertRepository>();

builder.Services.AddScoped<RegisterPatientUseCase>();
builder.Services.AddScoped<RegisterVitalReadingUseCase>();
builder.Services.AddScoped<ListPatientReadingsUseCase>();
builder.Services.AddScoped<ListPatientAlertsUseCase>();
builder.Services.AddScoped<AcknowledgeAlertUseCase>();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<VitaSignalDbContext>(tags: ["ready"]);

builder.AddVitaSignalTelemetry();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VitaSignalDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();
app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

await app.RunAsync();

public partial class Program;
