using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace VitaSignal.DeviceSimulator;

internal sealed class VitaSignalApiClient
{
    private readonly HttpClient _httpClient;

    public VitaSignalApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid> RegisterPatientAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(new Uri("api/patients", UriKind.Relative), content: null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var patient = await response.Content.ReadFromJsonAsync<RegisteredPatient>(cancellationToken)
            ?? throw new InvalidOperationException("Empty response when registering a patient.");

        return patient.Id;
    }

    public async Task<int> SendReadingAsync(
        Guid patientId, SimulatedReading reading, DateTime recordedAtUtc, string deviceId, CancellationToken cancellationToken)
    {
        var body = new ReadingPayload(patientId, reading.Type, reading.Value, recordedAtUtc, deviceId);

        using var response = await _httpClient.PostAsJsonAsync(
            new Uri("api/vitalreadings", UriKind.Relative), body, cancellationToken);

        return (int)response.StatusCode;
    }

    private sealed record RegisteredPatient(Guid Id);

    private sealed record ReadingPayload(
        Guid PatientId,
        [property: JsonConverter(typeof(JsonStringEnumConverter<VitalSign>))] VitalSign Type,
        double Value,
        DateTime RecordedAtUtc,
        string DeviceId);
}
