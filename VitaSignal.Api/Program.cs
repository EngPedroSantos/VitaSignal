using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using VitaSignal.Api;
using VitaSignal.Application.Patients;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Infrastructure.Patients;
using VitaSignal.Infrastructure.VitalReadings;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
builder.Services.AddSingleton<IVitalReadingRepository, InMemoryVitalReadingRepository>();

builder.Services.AddScoped<RegisterPatientUseCase>();
builder.Services.AddScoped<RegisterVitalReadingUseCase>();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

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