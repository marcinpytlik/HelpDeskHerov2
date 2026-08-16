using Xunit;

namespace HelpDeskHero.IntegrationTests.Common;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration tests";
}