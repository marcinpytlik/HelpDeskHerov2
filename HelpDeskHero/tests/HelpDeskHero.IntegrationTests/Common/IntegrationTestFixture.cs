using Xunit;

namespace HelpDeskHero.IntegrationTests.Common;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public CustomWebApplicationFactory Factory { get; } = new();

    public HttpClient Client { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await Factory.InitializeDatabaseAsync();

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        await Factory.DisposeAsync();
    }
}