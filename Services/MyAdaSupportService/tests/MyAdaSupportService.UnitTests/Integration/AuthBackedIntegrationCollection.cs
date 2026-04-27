using MyAda.Testing.Integration.Infrastructure;
using Xunit;

namespace MyAdaSupportService.UnitTests.Integration;

[CollectionDefinition("AuthBackedIntegration")]
public sealed class AuthBackedIntegrationCollection : ICollectionFixture<AuthIntegrationServer>
{
}
