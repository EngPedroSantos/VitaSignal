using System.Net;
using System.Net.Http.Json;
using VitaSignal.Api.Contracts;
using VitaSignal.Api.IntegrationTests.Support;
using VitaSignal.Domain.Alerts.Enums;

namespace VitaSignal.Api.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public class AlertsEndpointsTests
{
    private readonly ApiClient _api;

    public AlertsEndpointsTests(VitaSignalApiFactory factory)
    {
        _api = new ApiClient(factory.CreateClient());
    }

    [Fact]
    public async Task Should_RaiseCriticalAlert_When_HeartRateIsFarOutsideRange()
    {
        var patient = await _api.CreatePatientAsync();

        var result = await _api.RegisterReadingAsync(patient.Id, "HeartRate", 180);

        Assert.False(result.IsWithinNormalRange);
        Assert.NotNull(result.Alert);
        Assert.Equal(AlertSeverity.Critical, result.Alert.Severity);
        Assert.Equal(result.Reading.Id, result.Alert.ReadingId);
    }

    [Fact]
    public async Task Should_RemoveAlertFromPendingList_When_AlertIsAcknowledged()
    {
        var patient = await _api.CreatePatientAsync();
        var result = await _api.RegisterReadingAsync(patient.Id, "SpO2", 92);
        var pendingUrl = $"/api/patients/{patient.Id}/alerts?pendingOnly=true";

        var pendingBefore = await _api.GetAsync<PagedResponse<VitalAlertResponse>>(pendingUrl);
        Assert.Single(pendingBefore.Items);

        using var response = await _api.Http.PostAsync(
            new Uri($"/api/alerts/{result.Alert!.Id}/acknowledge", UriKind.Relative), content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var acknowledged = await response.Content.ReadFromJsonAsync<VitalAlertResponse>(ApiClient.JsonOptions);
        Assert.NotNull(acknowledged!.AcknowledgedAtUtc);

        var pendingAfter = await _api.GetAsync<PagedResponse<VitalAlertResponse>>(pendingUrl);
        Assert.Empty(pendingAfter.Items);

        var all = await _api.GetAsync<PagedResponse<VitalAlertResponse>>($"/api/patients/{patient.Id}/alerts");
        Assert.Single(all.Items);
    }

    [Fact]
    public async Task Should_FilterAlertsBySeverity_When_SeverityIsGiven()
    {
        var patient = await _api.CreatePatientAsync();
        await _api.RegisterReadingAsync(patient.Id, "HeartRate", 110);
        await _api.RegisterReadingAsync(patient.Id, "HeartRate", 180);

        var critical = await _api.GetAsync<PagedResponse<VitalAlertResponse>>($"/api/patients/{patient.Id}/alerts?severity=Critical");

        var alert = Assert.Single(critical.Items);
        Assert.Equal(180, alert.Value);
    }

    [Fact]
    public async Task Should_Return404_When_AcknowledgingUnknownAlert()
    {
        using var response = await _api.Http.PostAsync(
            new Uri($"/api/alerts/{Guid.NewGuid()}/acknowledge", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
