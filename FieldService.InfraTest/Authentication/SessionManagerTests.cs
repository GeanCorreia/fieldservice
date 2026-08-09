using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using FieldService.Authentication;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Services;
using FieldService.Authentication.Types;
using FieldService.Cache;
using FieldService.Cache.Interfaces;
using FieldService.Data.Interfaces;
using FieldService.InfraTest.Configuration;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.InfraTest.Authentication;

public sealed class LoginServiceTests
{
    [Fact]
    public async Task Should_login_only_and_persist_session_in_redis()
    {
        var fixture = AuthFixture.Create();
        var jwtId = "jwt-login-1";
        var expiresAt = fixture.Now.AddHours(1);
        var principal = fixture.CreatePrincipal(jwtId, expiresAt);
        var tenantId = Guid.NewGuid();
        var sessionId = await fixture.LoginAsync(principal, tenantId);

        Assert.Single(fixture.SessionRepository.SavedSessions);
        Assert.True(await fixture.ExistsKeyAsync(AuthFixture.SessionKey(sessionId)));
        Assert.Equal(jwtId, await fixture.ReadStringKeyAsync(AuthFixture.SessionJwtKey(sessionId)));
        Assert.NotNull(await fixture.ReadStringKeyAsync(AuthFixture.SessionLastActivityKey(sessionId)));
    }

    [Fact]
    public async Task Should_login_then_logout_and_remove_session_keys_from_redis()
    {
        var fixture = AuthFixture.Create();
        var jwtId = "jwt-logout-1";
        var expiresAt = fixture.Now.AddHours(1);
        var tenantId = Guid.NewGuid();
        var loginPrincipal = fixture.CreatePrincipal(jwtId, expiresAt);
        var sessionId = await fixture.LoginAsync(loginPrincipal, tenantId);

        var logoutPrincipal = fixture.CreatePrincipal(jwtId, expiresAt, sessionId);
        fixture.SetContext(logoutPrincipal, fixture.CreateRequestContext());
        await fixture.LoginService.LogoutAsync(RevocationReason.Logout, sessionId);

        Assert.True(fixture.SessionRepository.SavedSessions.Count >= 2);
        Assert.False(await fixture.ExistsKeyAsync(AuthFixture.SessionKey(sessionId)));
        Assert.False(await fixture.ExistsKeyAsync(AuthFixture.SessionJwtKey(sessionId)));
        Assert.False(await fixture.ExistsKeyAsync(AuthFixture.SessionLastActivityKey(sessionId)));
        Assert.False(await fixture.ExistsKeyAsync(AuthFixture.SessionActivitiesKey(sessionId)));
    }
}

public sealed class SessionManagerTests
{
    [Fact]
    public async Task Should_touch_with_same_jwt_then_change_jwt_and_expires_at_in_redis()
    {
        var fixture = AuthFixture.Create();
        var jwtId1 = "jwt-touch-1";
        var jwtId2 = "jwt-touch-2";
        var tenantId = Guid.NewGuid();
        var exp1 = fixture.Now.AddMinutes(30);
        var exp2 = fixture.Now.AddHours(2);
        var exp2FromClaim = DateTimeOffset.FromUnixTimeSeconds(((DateTimeOffset)exp2).ToUnixTimeSeconds()).UtcDateTime;

        var loginPrincipal = fixture.CreatePrincipal(jwtId1, exp1);
        var sessionId = await fixture.LoginAsync(loginPrincipal, tenantId);

        await fixture.Manager.TouchAsync();

        Assert.Equal(jwtId1, await fixture.ReadStringKeyAsync(AuthFixture.SessionJwtKey(sessionId)));

        await fixture.Manager.TouchAsync();

        Assert.Equal(jwtId2, await fixture.ReadStringKeyAsync(AuthFixture.SessionJwtKey(sessionId)));

        var sessionJson = await fixture.ReadStringKeyAsync(AuthFixture.SessionKey(sessionId));
        Assert.NotNull(sessionJson);
        using var json = JsonDocument.Parse(sessionJson!);
        var expiresAtInCache = json.RootElement.GetProperty("ExpiresAt").GetDateTime();
        Assert.Equal(exp2FromClaim, expiresAtInCache);
    }
}

internal sealed class AuthFixture
{
    public DateTime Now { get; }
    public UserAuthentication User { get; }
    public SessionRepositorySpy SessionRepository { get; }
    public LoginService LoginService { get; }
    public SessionManager Manager { get; }

    private readonly RequestContextManagerStub _contextManager;
    private readonly IRedisContext _redisContext;

    private AuthFixture(
        UserAuthentication user,
        DateTime now,
        SessionRepositorySpy sessionRepository,
        LoginService loginService,
        SessionManager manager,
        RequestContextManagerStub contextManager,
        IRedisContext redisContext)
    {
        User = user;
        Now = now;
        SessionRepository = sessionRepository;
        LoginService = loginService;
        Manager = manager;
        _contextManager = contextManager;
        _redisContext = redisContext;
    }

    public static AuthFixture Create()
    {
        var configuration = BootstrapConfigurationLoader.LoadDevelopmentConfiguration();
        var services = new ServiceCollection();
        services.AddCacheModule(configuration);
        services.AddAuthenticationModule(configuration);
        var provider = services.BuildServiceProvider();

        var uow = provider.GetRequiredService<IUnitOfWork>(); 
        var sessionRepository = new SessionRepositorySpy();
        var mapper = provider.GetRequiredService<ISessionMapper>();
        var sessionAuth = new SessionAuthenticationServiceStub(sessionRepository, mapper);
        var user = new UserAuthentication(
            Guid.NewGuid(), 
            Guid.NewGuid().ToString(), 
            AuthenticationProvider.AzureAd,
            []);
        var userAuth = new UserAuthenticationServiceStub(user);
        var now = DateTime.UtcNow;
        var dateTime = new FixedDateTimeService(now);
        var cache = provider.GetRequiredService<ISessionCacheService>();
        var redis = provider.GetRequiredService<IRedisContext>();
        var contextManager = new RequestContextManagerStub();


        var loginService = new LoginService(
            sessionRepository,
            sessionAuth,
            cache,
            mapper,
            userAuth,
            dateTime,
            contextManager,
            uow);
          

        var sessionManager = new SessionManager(
            mapper, cache, contextManager, loginService );

        return new AuthFixture(user, now, sessionRepository, loginService, sessionManager, contextManager, redis);
    }

    public void SetContext(ClaimsPrincipal principal, RequestContext context)
    {
        _contextManager.SetPrincipal(principal);
        _contextManager.Initialize(context);
    }

    public async Task<Guid> LoginAsync(ClaimsPrincipal principal, Guid tenantId)
    {
        SetContext(principal, CreateRequestContext());
        await LoginService.LoginAsync(tenantId);
        return GetSessionIdFromPrincipal(principal);
    }

    public ClaimsPrincipal CreatePrincipal(string jwtId, DateTime expiresAt, Guid? sessionId = null, Guid? tenantId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimsExtensions.UserId, User.UserId.ToString()),
            new(ClaimsExtensions.TenantId, tenantId?.ToString() ?? Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Exp, ToUnixSeconds(expiresAt).ToString())
        };

        if (sessionId.HasValue)
            claims.Add(new Claim(ClaimsExtensions.SessionId, sessionId.Value.ToString()));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, ClaimsExtensions.AuthenticationIdentity));
    }

    public RequestContext CreateRequestContext() =>
        RequestContext.Create( RequestChannel.Http, "127.0.0.1", "session-manager-tests");

    public static Guid GetSessionIdFromPrincipal(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimsExtensions.SessionId)?.Value;
        Assert.False(string.IsNullOrWhiteSpace(value));
        return Guid.Parse(value!);
    }

    public Task<bool> ExistsKeyAsync(string key) => _redisContext.Database.KeyExistsAsync(key);

    public async Task<string?> ReadStringKeyAsync(string key)
    {
        var value = await _redisContext.Database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public static string SessionKey(Guid sessionId) => $"session:{sessionId:N}";
    public static string SessionJwtKey(Guid sessionId) => $"session:{sessionId:N}:jwtId";
    public static string SessionLastActivityKey(Guid sessionId) => $"session:{sessionId:N}:lastActivity";
    public static string SessionActivitiesKey(Guid sessionId) => $"session:{sessionId:N}:activities";


    private static long ToUnixSeconds(DateTime dateTime) =>
        ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
}

internal sealed class RequestContextManagerStub : IRequestContextManager
{
    public RequestContext Request { get; private set; } = null!;
    public ClaimsPrincipal Principal { get; private set; } = null!;

    public void Initialize(RequestContext requestContext) => Request = requestContext;
    public void SetPrincipal(ClaimsPrincipal principal) => Principal = principal;
    public void Clear() { }
}

internal sealed class SessionRepositorySpy : ISessionRepository
{
    private readonly Dictionary<Guid, Session> _sessions = [];
    public List<Session> SavedSessions { get; } = [];

    public Task Save(Session session, CancellationToken ct = default)
    {
        SavedSessions.Add(session);
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task Save(IEnumerable<Session> sessions, CancellationToken ct = default)
    {
        foreach (var session in sessions)
        {
            SavedSessions.Add(session);
            _sessions[session.Id] = session;
        }
        return Task.CompletedTask;
    }

    public Task<Session?> GetById(Guid sessionId, CancellationToken ct = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }
}

internal sealed class SessionAuthenticationServiceStub(
    ISessionRepository sessionRepository,
    ISessionMapper sessionMapper) : ISessionAuthenticationService
{
    public async Task<SessionCacheModel?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetById(sessionId, cancellationToken);
        return session == null ? null : sessionMapper.Map(session);
    }
}

internal sealed class UserAuthenticationServiceStub(UserAuthentication user) : IUserAuthenticationService
{
    public Task<UserAuthentication?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(userId == user.UserId  ? user : null);

    public Task<UserAuthenticationCacheModel?> GetUserAsync(
        string sub,
        AuthenticationProvider provider,
        CancellationToken cancellationToken = default)
    {
        if (sub != user.ExternalId || provider != user.Provider)
        {
            return Task.FromResult<UserAuthenticationCacheModel?>(null);
        }

        return Task.FromResult<UserAuthenticationCacheModel?>(
            new UserAuthenticationCacheModel(
                user.UserId,
                user.ExternalId,
                user.Provider,
                []));
    }

    public Task UpdateUserAsync(UserAuthentication userAuthentication, CancellationToken ct = default) =>
        Task.CompletedTask;
}

internal sealed class FixedDateTimeService(DateTime now) : IDateTimeService
{
    public DateTime Now() => now;
    public DateOnly Today() => DateOnly.FromDateTime(now);
}
