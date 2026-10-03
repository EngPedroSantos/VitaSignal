using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace VitaSignal.Api.IntegrationTests.Support;

public sealed class VitaSignalApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17.11")
        .WithDatabase("vitasignal")
        .WithUsername("vitasignal")
        .WithPassword("vitasignal")
        .Build();

    public string ConnectionString => _database.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:VitaSignalDb", ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiTestGroup : ICollectionFixture<VitaSignalApiFactory>
{
    public const string Name = "api";
}
