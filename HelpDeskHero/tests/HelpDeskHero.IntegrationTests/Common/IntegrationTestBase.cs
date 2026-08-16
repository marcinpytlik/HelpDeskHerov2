using Xunit;

namespace HelpDeskHero.IntegrationTests.Common;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    protected HttpClient Client => _fixture.Client;

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.Factory.ClearTestDataAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}