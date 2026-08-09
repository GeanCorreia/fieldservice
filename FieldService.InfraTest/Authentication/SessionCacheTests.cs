using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Authentication;
using FieldService.Cache;
using FieldService.Cache.Interfaces;
using FieldService.InfraTest.Configuration;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.InfraTest.Authentication;

public sealed class SessionCacheTests
{
    [Fact]
    public async Task Should_roundtrip_session_through_cache()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();
        var services = new ServiceCollection();
        services.AddCacheModule(configuration);
        services.AddAuthenticationModule(configuration);

        using var provider = services.BuildServiceProvider();

        var cacheService = provider.GetRequiredService<ISessionCacheService>();
        var mapper = provider.GetRequiredService<ISessionMapper>();

        var userAuthentication = new UserAuthentication(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            AuthenticationProvider.AzureAd,
            []);

        var now = DateTimeService.GetNow();
        var session = new Session(
            Array.Empty<SessionActivity>(),
            Guid.NewGuid(),
            userAuthentication.UserId,
            Guid.NewGuid(),
            userAuthentication.ExternalId,
            userAuthentication.Provider,
            now.AddMinutes(-5),
            now.AddMinutes(55));

        await cacheService.SaveSessionAsync(mapper.Map(session));

        var loadedCacheModel = await cacheService.GetSessionAsync(session.Id);
        var loaded = loadedCacheModel is null ? null : mapper.Map(loadedCacheModel);
        Assert.NotNull(loaded);
        Assert.Equal(session.Id, loaded.Id);
        Assert.Equal(session.UserId, loaded.UserId);
        Assert.Equal(session.TenantId, loaded.TenantId);

    }

    [Fact]
    public async Task Should_reconstruct_session_with_five_activities_from_cache()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();
        var services = new ServiceCollection();
        services.AddCacheModule(configuration);
        services.AddAuthenticationModule(configuration);

        using var provider = services.BuildServiceProvider();

        var cacheService = provider.GetRequiredService<ISessionCacheService>();
        var mapper = provider.GetRequiredService<ISessionMapper>();

        var userAuthentication = new UserAuthentication(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            AuthenticationProvider.AzureAd,
            []);

        var now = DateTimeService.GetNow();
        var sessionId = Guid.NewGuid();
        var jwtId = Guid.NewGuid().ToString("N");
        var activities = Enumerable.Range(1, 5)
            .Select(index => new SessionActivity(
                Guid.NewGuid(),
                sessionId,
                jwtId,
                $"ip-hash-{index}",
                now.AddMinutes(-index),
                index % 2 == 0 ? RequestChannel.Http : RequestChannel.WebSocket,
                Guid.NewGuid(),
                $"user-agent-hash-{index}"))
            .ToArray();

        var session = new Session(
            Array.Empty<SessionActivity>(),
            sessionId,
            userAuthentication.UserId,
            Guid.NewGuid(),
            userAuthentication.ExternalId,
            userAuthentication.Provider,
            now.AddMinutes(-10),
            now.AddHours(1));

        foreach (var activity in activities)
            session.Touch(activity);

        await cacheService.SaveSessionAsync(mapper.Map(session));

        var loadedCacheModel = await cacheService.GetSessionAsync(sessionId);
        var loaded = loadedCacheModel is null ? null : mapper.Map(loadedCacheModel);

        Assert.NotNull(loaded);
        Assert.Equal(5, loaded.Activities.Count);
        Assert.NotNull(loaded.LastActivityAt);
        Assert.Equal(activities.Last().Timestamp, loaded.LastActivityAt);
        Assert.Equal(session.UserId, loaded.UserId);
        Assert.Equal(session.TenantId, loaded.TenantId);
        Assert.All(loaded.Activities, activity => Assert.Equal(sessionId, activity.SessionId));
    }

    [Fact]
    public async Task Should_save_and_remove_session_from_cache()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();
        var services = new ServiceCollection();
        services.AddCacheModule(configuration);
        services.AddAuthenticationModule(configuration);

        using var provider = services.BuildServiceProvider();

        var cacheService = provider.GetRequiredService<ISessionCacheService>();

        var cacheModel = new SessionCacheModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "external-user-2",
            AuthenticationProvider.AzureAd,
            DateTimeService.GetNow().AddMinutes(-1),
            DateTimeService.GetNow().AddMinutes(59),
            null,
            null,
            null,
            Array.Empty<SessionActivityCacheModel>());

        await cacheService.SaveSessionAsync(cacheModel);

        var loaded = await cacheService.GetSessionAsync(cacheModel.Id);
        Assert.NotNull(loaded);
        Assert.Equal(cacheModel.Id, loaded.Id);

        Assert.True(await cacheService.ExistsAsync(cacheModel.Id));

        await cacheService.RemoveSessionAsync(cacheModel.Id);

        Assert.False(await cacheService.ExistsAsync(cacheModel.Id));
        Assert.Null(await cacheService.GetSessionAsync(cacheModel.Id));
    }

    [Fact]
    public async Task Should_get_inactive_candidates_based_on_last_activity()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();
        var services = new ServiceCollection();
        services.AddCacheModule(configuration);
        services.AddAuthenticationModule(configuration);

        using var provider = services.BuildServiceProvider();

        var cacheService = provider.GetRequiredService<ISessionCacheService>();
        var mapper = provider.GetRequiredService<ISessionMapper>();
        var redisContext = provider.GetRequiredService<IRedisContext>();

        // Limpar sessões antigas de testes anteriores
        var server = redisContext.Connection.GetServer(redisContext.Connection.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: "session:*"))
        {
            await redisContext.Database.KeyDeleteAsync(key);
        }

        var userAuthentication1 = new UserAuthentication(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            AuthenticationProvider.AzureAd,
            []);

        var userAuthentication2 = new UserAuthentication(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            AuthenticationProvider.AzureAd,
            []);

        var now = DateTimeService.GetNow();
        var inactiveActivityTime = now.AddMinutes(-25);
        var activeActivityTime = now.AddMinutes(-5);
        
        var tenantActiveSession = Guid.NewGuid();
        var tenantInactiveSession = Guid.NewGuid();

        // Session inativa (última atividade há 25 minutos)
        var inactiveSession = new Session(
            Array.Empty<SessionActivity>(),
            Guid.NewGuid(),
            userAuthentication1.UserId,
            tenantInactiveSession,
            userAuthentication1.ExternalId,
            userAuthentication1.Provider,
            now.AddMinutes(-30),
            now.AddHours(1));
        
        inactiveSession.Touch(new SessionActivity(
            Guid.NewGuid(),
            inactiveSession.Id,
            Guid.NewGuid().ToString("N"),
            "ip-hash-inactive",
            inactiveActivityTime,
            RequestChannel.Http,
            Guid.NewGuid(),
            "user-agent-hash-inactive"));

        // Session ativa (última atividade há 5 minutos)
        var activeSession = new Session(
            Array.Empty<SessionActivity>(),
            Guid.NewGuid(),
            userAuthentication2.UserId,
            tenantActiveSession,
            userAuthentication2.ExternalId,
            userAuthentication2.Provider,
            now.AddMinutes(-10),
            now.AddHours(1));
        
        activeSession.Touch(new SessionActivity(
            Guid.NewGuid(),
            activeSession.Id,
            Guid.NewGuid().ToString("N"),
            "ip-hash-active",
            activeActivityTime,
            RequestChannel.WebSocket,
            Guid.NewGuid(),
            "user-agent-hash-active"));

        await cacheService.SaveSessionAsync(mapper.Map(inactiveSession));
        await cacheService.SaveSessionAsync(mapper.Map(activeSession));

        // Buscar sessões inativas com threshold de 20 minutos
        var inactiveCandidates = await cacheService.GetInactiveCandidatesAsync(TimeSpan.FromMinutes(20));
        var candidates = inactiveCandidates.ToList();

        Assert.Single(candidates);
        Assert.Equal(inactiveSession.Id, candidates[0].Id);
        Assert.Equal(inactiveSession.LastActivityAt, candidates[0].LastActivityAt);

        // Limpar cache
        await cacheService.RemoveSessionAsync(inactiveSession.Id);
        await cacheService.RemoveSessionAsync(activeSession.Id);
    }

    [Fact]
    public async Task Should_touch_session_with_new_activity_without_loading_full_entity()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();
        var services = new ServiceCollection();
        services.AddCacheModule(configuration);
        services.AddAuthenticationModule(configuration);

        using var provider = services.BuildServiceProvider();

        var cacheService = provider.GetRequiredService<ISessionCacheService>();
        var mapper = provider.GetRequiredService<ISessionMapper>();

        var userAuthentication = new UserAuthentication(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            AuthenticationProvider.AzureAd,
            []);

        var now = DateTimeService.GetNow();
        var session = new Session(
            Array.Empty<SessionActivity>(),
            Guid.NewGuid(),
            userAuthentication.UserId,
            Guid.NewGuid(),
            userAuthentication.ExternalId,
            userAuthentication.Provider,
            now.AddMinutes(-10),
            now.AddHours(1));

        var initialActivity = new SessionActivity(
            Guid.NewGuid(),
            session.Id,
            Guid.NewGuid().ToString("N"),
            "ip-hash-initial",
            now.AddMinutes(-5),
            RequestChannel.Http,
            Guid.NewGuid(),
            "user-agent-hash-initial");

        session.Touch(initialActivity);

        await cacheService.SaveSessionAsync(mapper.Map(session));

        var newActivityTimestamp = DateTimeService.GetNow();
        var newActivity = new SessionActivityCacheModel(
            Guid.NewGuid(),
            session.Id,
            Guid.NewGuid().ToString("N"),
            "ip-hash-new",
            "user-agent-hash-new",
            newActivityTimestamp,
            RequestChannel.WebSocket,
            Guid.NewGuid());

        await cacheService.TouchSessionAsync(session.Id, newActivity);

        var loadedCacheModel = await cacheService.GetSessionAsync(session.Id);
        var loaded = loadedCacheModel is null ? null : mapper.Map(loadedCacheModel);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Activities.Count);
        Assert.Equal(newActivityTimestamp, loaded.LastActivityAt);
        Assert.Contains(loaded.Activities, a => a.Id == newActivity.Id);
        Assert.Contains(loaded.Activities, a => a.Id == initialActivity.Id);

        await cacheService.RemoveSessionAsync(session.Id);
    }

    [Fact]
    public async Task Should_throw_when_touching_non_existent_session()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();
        var services = new ServiceCollection();
        services.AddCacheModule(configuration);
        services.AddAuthenticationModule(configuration);

        using var provider = services.BuildServiceProvider();

        var cacheService = provider.GetRequiredService<ISessionCacheService>();

        var nonExistentSessionId = Guid.NewGuid();
        var activity = new SessionActivityCacheModel(
            Guid.NewGuid(),
            nonExistentSessionId,
            Guid.NewGuid().ToString("N"),
            "ip-hash",
            "user-agent-hash",
            DateTimeService.GetNow(),
            RequestChannel.Http,
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await cacheService.TouchSessionAsync(nonExistentSessionId, activity));

        Assert.Contains(nonExistentSessionId.ToString(), exception.Message);
    }
}
