using System.Net;
using VitaSignal.Api.IntegrationTests.Support;

namespace VitaSignal.Api.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public class OperationalEndpointsTests
{
    private readonly ApiClient _api;

    public OperationalEndpointsTests(VitaSignalApiFactory factory)
    {
        _api = new ApiClient(factory.CreateClient());
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Should_ReportHealthy_When_DatabaseIsAvailable(string path)
    {
        using var response = await _api.Http.GetAsync(new Uri(path, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_ExposeBusinessMetrics_When_ReadingsAreRegistered()
    {
        var patient = await _api.CreatePatientAsync();
        await _api.RegisterReadingAsync(patient.Id, "HeartRate", 180);

        using var response = await _api.Http.GetAsync(new Uri("/metrics", UriKind.Relative));
        var metrics = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("vitasignal_vital_readings_registered_total", metrics, StringComparison.Ordinal);
        Assert.Contains("vitasignal_vital_readings_out_of_range_total", metrics, StringComparison.Ordinal);
        Assert.Contains("vitasignal_alerts_raised_total", metrics, StringComparison.Ordinal);
    }
}
