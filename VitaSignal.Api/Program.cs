using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using VitaSignal.Api;
using VitaSignal.Application.Patients;
using VitaSignal.Application.VitalReadings;
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
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IVitalReadingRepository, VitalReadingRepository>();

builder.Services.AddScoped<RegisterPatientUseCase>();
builder.Services.AddScoped<RegisterVitalReadingUseCase>();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

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
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();