using FieldService.InfraTest.Configuration;
using Microsoft.Extensions.Configuration;

namespace FieldService.InfraTest.Authorization;

public sealed class AuthorizationConfigurationTests
{
    [Fact]
    public void Should_read_authorization_cache_expiration_from_bootstrap_development_settings()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();

        var cacheExpirationInSeconds = configuration.GetValue<int>("Authorization:CacheExpirationInSeconds");

        Assert.Equal(86400, cacheExpirationInSeconds);
    }
}
