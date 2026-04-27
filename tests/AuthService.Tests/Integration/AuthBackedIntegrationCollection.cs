using MyAda.Testing.Integration.Infrastructure;
using Xunit;

namespace AuthService.Tests.Integration;

[CollectionDefinition("AuthBackedIntegration")]
public sealed class AuthBackedIntegrationCollection : ICollectionFixture<AuthIntegrationServer>
{
}
