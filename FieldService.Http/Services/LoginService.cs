using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Services;
using FieldService.Authentication.Types;
using FieldService.Data.Interfaces;
using FieldService.Http.Interfaces;
using FieldService.Observability.Types;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;
using IAuthorizationService = FieldService.Authorization.Interfaces.IAuthorizationService;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;
using UserAuthenticationDto = FieldService.Http.Dtos.UserAuthenticationDto;

namespace FieldService.Http.Services;

public class LoginService : ILoginService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionAuthenticationService _sessionAuthenticationService;
    private readonly ISessionCacheService _sessionCacheService;
    private readonly ISessionMapper _sessionMapper;
    private readonly IAuthenticationService _authenticationService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserAuthenticationMapper _userMapper;


    public LoginService(
        ISessionRepository sessionRepository,
        ISessionAuthenticationService sessionAuthenticationService,
        ISessionCacheService sessionCacheService,
        ISessionMapper sessionMapper,
        IAuthenticationService authenticationService,
        IDateTimeService dateTimeService,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        IUserAuthenticationMapper userMapper,
        IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _sessionAuthenticationService = sessionAuthenticationService ?? throw new ArgumentNullException(nameof(sessionAuthenticationService));
        _sessionCacheService = sessionCacheService ?? throw new ArgumentNullException(nameof(sessionCacheService));
        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _dateTimeService = dateTimeService ?? throw new ArgumentNullException(nameof(dateTimeService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userMapper = userMapper ?? throw new ArgumentNullException(nameof(userMapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Guid> LoginAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var principal = GetHttpContext().User;
        var sessionId = Guid.NewGuid();
        var userId = ClaimsResolver.GetUserId(principal);
        

        var user = await _authenticationService
            .GetUserAsync(
                userId,
                cancellationToken);


        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }
        
        var userTenants = user.Tenants.ToList();
        
        if (userTenants.Count(t => t.TenantId == tenantId) == 0)
        {
            throw new UnauthorizedAccessException("User does not have access to the specified tenant.");
        }
        
        var expiresAt = ClaimsResolver.GetExpiresAt(principal);
        
        var jwtId = ClaimsResolver.GetJwtId(principal);
        var sub = ClaimsResolver.GetSubjectId(principal);
        var provider = (AuthenticationProvider)Enum.Parse(typeof(AuthenticationProvider), ClaimsResolver.GetProvider(principal));
        
        
        var session = new Session(
            id: sessionId,
            tenantId: tenantId,
            userId: userId,
            externalId: sub,
            provider: provider,
            startedAt: _dateTimeService.Now(),
            expiresAt: expiresAt);
        
        var sessionCacheModel = _sessionMapper.Map(session);

        var requestId = ObservabilityExecutionContext.RequestId ?? Guid.Empty;

        var sessionActivityCacheModel = new SessionActivityCacheModel(
            Guid.NewGuid(),
            sessionId,
            jwtId,
            ObservabilityExecutionContext.IpAddress,
            HashService.CreateHashSha256(ObservabilityExecutionContext.UserAgent),
            DateTime.UtcNow,
            RequestChannel.Http,
            requestId
        );
        

        ObservabilityExecutionContext.TenantId = tenantId;
        ObservabilityExecutionContext.SessionId = session.Id;

        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            await _sessionRepository.Save(
                session,
                cancellationToken);

            await _sessionCacheService.SaveSessionAsync(
                sessionCacheModel,
                cancellationToken);
            
            await _sessionCacheService.TouchSessionAsync(
                sessionCacheModel.Id,
                sessionActivityCacheModel,
                cancellationToken);
            
            
            var identity = ClaimsResolver.GetOrCreateAuthenticationIdentity(principal);
            ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.SessionId, 
                sessionCacheModel.Id.ToString());
            ClaimsResolver.UpsertClaim(identity, ClaimsExtensions.TenantId, 
                sessionCacheModel.TenantId.ToString());
            return session.Id;
        }
        catch 
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
            
        }
        
    } 
    
    public async Task LogoutAsync(
        
        RevocationReason reason,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == null)
        {
            var principal = GetHttpContext().User;
            sessionId = ClaimsResolver.GetSessionId(principal);

            if (sessionId == null)
            {
                throw new UnauthorizedAccessException("Session not found.");
            }
        }
      
        var sessionCacheModel = await _sessionCacheService
            .GetSessionAsync(sessionId.Value, cancellationToken);
        
        if (sessionCacheModel == null)
        {
            throw new InvalidOperationException("Session not found.");
        }
        
        var session = _sessionMapper.Map(sessionCacheModel);
        var revokedAt = _dateTimeService.Now();
        
        session.Revoke(revokedAt, reason);
        
        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            await _sessionRepository.Save(
                session,
                cancellationToken);

            await _sessionCacheService.RemoveSessionAsync(
                sessionId.Value, 
                cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
           
    }

    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
}
