using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Data.Interfaces;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.Authentication.Services;

public class LoginService : ILoginService
{
    private readonly IRequestContextManager _requestContextManager;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionAuthenticationService _sessionAuthenticationService;
    private readonly ISessionCacheService _sessionCacheService;
    private readonly ISessionMapper _sessionMapper;
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUnitOfWork _unitOfWork;


    public LoginService(
        ISessionRepository sessionRepository,
        ISessionAuthenticationService sessionAuthenticationService,
        ISessionCacheService sessionCacheService,
        ISessionMapper sessionMapper,
        IUserAuthenticationService userAuthenticationService,
        IDateTimeService dateTimeService,
        IRequestContextManager requestContextManager,
        IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _sessionAuthenticationService = sessionAuthenticationService ?? throw new ArgumentNullException(nameof(sessionAuthenticationService));
        _sessionCacheService = sessionCacheService ?? throw new ArgumentNullException(nameof(sessionCacheService));
        _sessionMapper = sessionMapper ?? throw new ArgumentNullException(nameof(sessionMapper));
        _userAuthenticationService = userAuthenticationService ?? throw new ArgumentNullException(nameof(userAuthenticationService));
        _dateTimeService = dateTimeService ?? throw new ArgumentNullException(nameof(dateTimeService));
        _requestContextManager = requestContextManager ?? throw new ArgumentNullException(nameof(requestContextManager));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Guid> LoginAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var context = _requestContextManager.Request;
        var principal = _requestContextManager.Principal;
        var sessionId = Guid.NewGuid();
        var userId = ClaimsResolver.GetUserId(principal);
        

        var user = await _userAuthenticationService.GetUserAsync(
            userId,
            cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }
        
        var userTenants = user.Tenants;

        if (userTenants.Count == 0)
        {
            throw new UnauthorizedAccessException("User has no tenants.");
        }

        if (userTenants.Count(t => t.TenantId == tenantId) == 0)
        {
            throw new UnauthorizedAccessException("User does not have access to the specified tenant.");
        }
        
        var expiresAt = ClaimsResolver.GetExpiresAt(principal);
        
        var jwtId = ClaimsResolver.GetJwtId(principal);
        
        var loginActivity = SessionManager.CreateSessionActivity(
            sessionId,
            jwtId,
            context);

        var session = new Session(
            activities: [loginActivity],
            id: sessionId,
            tenantId: tenantId,
            userId: userId,
            externalId: user.ExternalId,
            provider: user.Provider,
            startedAt: _dateTimeService.Now(),
            expiresAt: expiresAt);
        
        var sessionCacheModel = _sessionMapper.Map(session);

        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            await _sessionRepository.Save(
                session,
                cancellationToken);

            await _sessionCacheService.SaveSessionAsync(
                sessionCacheModel,
                cancellationToken);
            context.SessionId = session.Id;
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
    
    public async Task<IEnumerable<UserTenantAuthenticationCacheModel>> GetUserTenantsAsync(
        CancellationToken cancellationToken = default)
    {

        var principal = _requestContextManager.Principal;
        var userId = ClaimsResolver.GetUserId(principal);
        
        var user = await _userAuthenticationService.GetUserAsync(
            userId,
            cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        return user.Tenants.Select(t => new UserTenantAuthenticationCacheModel(
            t.TenantId,
            t.Name));
    }

    public async Task LogoutAsync(
        
        RevocationReason reason,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == null)
        {
            var principal = _requestContextManager.Principal;
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
}
