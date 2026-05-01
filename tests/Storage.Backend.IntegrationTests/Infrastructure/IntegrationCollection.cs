namespace Storage.Backend.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    public const string Name = "integration";
}
