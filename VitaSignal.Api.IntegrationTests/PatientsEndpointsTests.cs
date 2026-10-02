using System.Net;
using VitaSignal.Api.Contracts;
using VitaSignal.Api.IntegrationTests.Support;

namespace VitaSignal.Api.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public class PatientsEndpointsTests
{
    private readonly ApiClient _api;

    public PatientsEndpointsTests(VitaSignalApiFactory factory)
    {
        _api = new ApiClient(factory.CreateClient());
    }

    [Fact]
    public async Task Should_ReturnPseudonymousPatient_When_PatientIsCreatedAndFetched()
    {
        var created = await _api.CreatePatientAsync();

        var fetched = await _api.GetAsync<PatientResponse>($"/api/patients/{created.Id}");

        Assert.Equal(created, fetched);
        Assert.StartsWith("PAC-", fetched.Code, StringComparison.Ordinal);
        Assert.True(fetched.IsSyntheticData);
    }

    [Fact]
    public async Task Should_Return404WithTraceId_When_PatientDoesNotExist()
    {
        using var response = await _api.Http.GetAsync(new Uri($"/api/patients/{Guid.NewGuid()}/readings", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await ApiClient.ReadProblemAsync(response);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }
}
