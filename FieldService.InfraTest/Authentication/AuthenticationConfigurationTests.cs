using FieldService.Authentication;
using FieldService.Authentication.Types;
using FieldService.InfraTest.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.InfraTest.Authentication;

public sealed class AuthenticationConfigurationTests
{
    [Fact]
    public void Should_bind_authentication_session_from_bootstrap_development_settings()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();

        var options = configuration
            .GetSection(AuthenticationOptions.SectionName)
            .Get<AuthenticationOptions>();

        Assert.NotNull(options);
        Assert.NotNull(options.Session);
        Assert.Equal(120, options.Session.InactivityTimeoutInMinutes);
        Assert.Equal(60, options.Session.TokenLifetimeInMinutes);
        Assert.Equal(30, options.Session.PersistenceInactivityThresholdInMinutes);
        Assert.Equal(2, options.Session.CacheTtlExtraHours);
    }

    [Fact]
    public void Should_register_bound_authentication_options_in_di()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();
        var services = new ServiceCollection();

        services.AddAuthenticationModule(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<AuthenticationOptions>();

        Assert.NotNull(options);
        Assert.NotNull(options.Session);
        Assert.Equal(120, options.Session.InactivityTimeoutInMinutes);
        Assert.Equal(60, options.Session.TokenLifetimeInMinutes);
        Assert.Equal(30, options.Session.PersistenceInactivityThresholdInMinutes);
        Assert.Equal(2, options.Session.CacheTtlExtraHours);
    }
}
