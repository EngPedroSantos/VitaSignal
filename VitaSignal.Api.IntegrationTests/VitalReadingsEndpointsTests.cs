using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using VitaSignal.Api.Contracts;
using VitaSignal.Api.IntegrationTests.Support;
using VitaSignal.Infrastructure.Persistence;

namespace VitaSignal.Api.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public class VitalReadingsEndpointsTests
{
    private readonly VitaSignalApiFactory _factory;
    private readonly ApiClient _api;

    public VitalReadingsEndpointsTests(VitaSignalApiFactory factory)
    {
        _factory = factory;
        _api = new ApiClient(factory.CreateClient());
    }

    private static string IsoUtc(DateTime value) => value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    [Fact]
    public async Task Should_Return201WithLocation_When_ReadingIsValid()
    {
        var patient = await _api.CreatePatientAsync();

        using var response = await _api.PostReadingAsync(new
        {
            patientId = patient.Id,
            type = "HeartRate",
            value = 72,
            recordedAtUtc = IsoUtc(DateTime.UtcNow.AddMinutes(-1)),
            deviceId = "integration-monitor"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterVitalReadingResponse>(ApiClient.JsonOptions);
        Assert.NotNull(body);
        Assert.True(body.IsWithinNormalRange);
        Assert.Null(body.Alert);

        var stored = await _api.GetAsync<VitalReadingResponse>(response.Headers.Location!.PathAndQuery);
        Assert.Equal(body.Reading, stored);
    }

    [Fact]
    public async Task Should_StoreUtcTime_When_RecordedAtHasOffset()
    {
        var patient = await _api.CreatePatientAsync();
        var expectedUtc = DateTime.UtcNow.AddMinutes(-10);
        var withOffset = expectedUtc.AddHours(-3).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) + "-03:00";

        using var response = await _api.PostReadingAsync(new
        {
            patientId = patient.Id,
            type = "SpO2",
            value = 97,
            recordedAtUtc = withOffset,
            deviceId = "integration-monitor"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterVitalReadingResponse>(ApiClient.JsonOptions);
        Assert.Equal(expectedUtc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            body!.Reading.RecordedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
    }

    public static TheoryData<string, string> InvalidReadings => new()
    {
        { "no timezone", """{"type":"HeartRate","value":70,"recordedAtUtc":"2026-10-01T10:00:00","deviceId":"d1"}""" },
        { "missing recordedAtUtc", """{"type":"HeartRate","value":70,"deviceId":"d1"}""" },
        { "missing type", """{"value":70,"recordedAtUtc":"2026-10-01T10:00:00Z","deviceId":"d1"}""" },
        { "unknown type", """{"type":999,"value":70,"recordedAtUtc":"2026-10-01T10:00:00Z","deviceId":"d1"}""" },
        { "implausible value", """{"type":"SpO2","value":250,"recordedAtUtc":"2026-10-01T10:00:00Z","deviceId":"d1"}""" },
        { "device id too long", "{\"type\":\"HeartRate\",\"value\":70,\"recordedAtUtc\":\"2026-10-01T10:00:00Z\",\"deviceId\":\"" + new string('x', 101) + "\"}" },
        { "future reading", """{"type":"HeartRate","value":70,"recordedAtUtc":"2099-01-01T00:00:00Z","deviceId":"d1"}""" }
    };

    [Theory]
    [MemberData(nameof(InvalidReadings))]
    public async Task Should_Return400WithProblemDetails_When_ReadingIsInvalid(string scenario, string partialJson)
    {
        var patient = await _api.CreatePatientAsync();
        var json = partialJson.Insert(1, $"\"patientId\":\"{patient.Id}\",");

        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await _api.Http.PostAsync(new Uri("/api/vitalreadings", UriKind.Relative), content);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{scenario}: expected 400, got {(int)response.StatusCode}");
        var problem = await ApiClient.ReadProblemAsync(response);
        Assert.Equal(400, problem.Status);
        Assert.True(problem.Extensions.ContainsKey("traceId"), scenario);
        Assert.DoesNotContain("Parameter", problem.Detail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_NotStoreAnything_When_ReadingIsInvalid()
    {
        var patient = await _api.CreatePatientAsync();

        using var response = await _api.PostReadingAsync(new
        {
            patientId = patient.Id,
            type = "HeartRate",
            value = 70,
            deviceId = "integration-monitor"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var page = await _api.GetAsync<PagedResponse<VitalReadingResponse>>($"/api/patients/{patient.Id}/readings");
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task Should_Return404_When_PatientDoesNotExist()
    {
        using var response = await _api.PostReadingAsync(new
        {
            patientId = Guid.NewGuid(),
            type = "HeartRate",
            value = 70,
            recordedAtUtc = IsoUtc(DateTime.UtcNow.AddMinutes(-1)),
            deviceId = "integration-monitor"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_PageThroughReadingsNewestFirst_When_ReadingsShareTheSameTimestamp()
    {
        var patient = await _api.CreatePatientAsync();
        var sameTimestamp = DateTime.UtcNow.AddMinutes(-2);
        var older = sameTimestamp.AddMinutes(-1);

        for (var i = 0; i < 4; i++)
            await _api.RegisterReadingAsync(patient.Id, "HeartRate", 70 + i, sameTimestamp);
        await _api.RegisterReadingAsync(patient.Id, "HeartRate", 90, older);

        var seen = new List<VitalReadingResponse>();
        string? cursor = null;
        var pages = 0;

        do
        {
            var url = $"/api/patients/{patient.Id}/readings?limit=2" + (cursor is null ? "" : $"&cursor={cursor}");
            var page = await _api.GetAsync<PagedResponse<VitalReadingResponse>>(url);
            Assert.True(page.Items.Count <= 2);

            seen.AddRange(page.Items);
            cursor = page.NextCursor;
            pages++;
        }
        while (cursor is not null);

        Assert.Equal(3, pages);
        Assert.Equal(5, seen.Select(r => r.Id).Distinct().Count());
        Assert.Equal(90, seen[^1].Value);
        Assert.Equal(seen.OrderByDescending(r => r.RecordedAtUtc).Select(r => r.RecordedAtUtc), seen.Select(r => r.RecordedAtUtc));
    }

    [Fact]
    public async Task Should_FilterByType_When_TypeIsGiven()
    {
        var patient = await _api.CreatePatientAsync();
        await _api.RegisterReadingAsync(patient.Id, "HeartRate", 70);
        await _api.RegisterReadingAsync(patient.Id, "SpO2", 98);

        var page = await _api.GetAsync<PagedResponse<VitalReadingResponse>>($"/api/patients/{patient.Id}/readings?type=SpO2");

        var reading = Assert.Single(page.Items);
        Assert.Equal(98, reading.Value);
    }

    [Theory]
    [InlineData("limit=0")]
    [InlineData("limit=501")]
    [InlineData("cursor=not-a-cursor")]
    public async Task Should_Return400_When_PaginationParametersAreInvalid(string query)
    {
        var patient = await _api.CreatePatientAsync();

        using var response = await _api.Http.GetAsync(new Uri($"/api/patients/{patient.Id}/readings?{query}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_RejectReadingForUnknownPatient_When_InsertedDirectlyInDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VitaSignalDbContext>();

        var exception = await Assert.ThrowsAsync<PostgresException>(() => dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "VitalReadings" ("Id", "PatientId", "Type", "Value", "Unit", "RecordedAtUtc", "DeviceId")
            VALUES ({Guid.NewGuid()}, {Guid.NewGuid()}, 'HeartRate', 70, 'bpm', now(), 'orphan')
            """));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
    }
}
