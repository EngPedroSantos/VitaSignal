using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using VitaSignal.Api.Contracts;

namespace VitaSignal.Api.IntegrationTests.Support;

public sealed class ApiClient
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public HttpClient Http => _httpClient;

    public async Task<PatientResponse> CreatePatientAsync()
    {
        using var response = await _httpClient.PostAsync(new Uri("/api/patients", UriKind.Relative), content: null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<PatientResponse>(JsonOptions))!;
    }

    public Task<HttpResponseMessage> PostReadingAsync(object body) =>
        _httpClient.PostAsJsonAsync(new Uri("/api/vitalreadings", UriKind.Relative), body, JsonOptions);

    public async Task<RegisterVitalReadingResponse> RegisterReadingAsync(
        Guid patientId, string type, double value, DateTime? recordedAtUtc = null)
    {
        using var response = await PostReadingAsync(new
        {
            patientId,
            type,
            value,
            recordedAtUtc = recordedAtUtc ?? DateTime.UtcNow.AddMinutes(-1),
            deviceId = "integration-monitor"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RegisterVitalReadingResponse>(JsonOptions))!;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        using var response = await _httpClient.GetAsync(new Uri(url, UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    public static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions))!;
}
