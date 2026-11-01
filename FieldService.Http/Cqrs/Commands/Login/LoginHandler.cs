using System.Security.Claims;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Data.Interfaces;
using FieldService.Http.Cqrs.Queries.GetUser;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Http.Cqrs.Commands.Login;

public class LoginHandler : IRequestHandler<LoginCommand, Guid>
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    
    public LoginHandler(
        ILogger<LoginHandler> logger, 
        ISessionService sessionService,
        IUnitOfWork unitOfWork,
        IMediator mediator
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<Guid> Handle(
        LoginCommand request, 
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            var sessionId = Guid.NewGuid();
            var userId = ClaimsResolver.GetUserId(request.User);
            var tenantId = request.TenantId;
            var expiresAt = ClaimsResolver.GetExpiresAt(request.User);
            var jwtId = ClaimsResolver.GetJwtId(request.User);
            var sub = ClaimsResolver.GetSubjectId(request.User);
            var provider = (AuthenticationProvider)Enum.Parse(typeof(AuthenticationProvider),
                ClaimsResolver.GetProvider(request.User));


            var user = await _mediator.Send(new GetUserQuery(userId), cancellationToken);

            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var userTenants = user.Tenants.ToList();

            if (userTenants.Count(t => t.TenantId == tenantId) == 0)
            {
                throw new UnauthorizedAccessException("User does not have access to the specified tenant.");
            }
            
            ClaimsResolver.UpsertClaim(request.User.Identity as ClaimsIdentity, ClaimsExtensions.TenantId, tenantId.ToString());
            ClaimsResolver.UpsertClaim(request.User.Identity as ClaimsIdentity, ClaimsExtensions.SessionId, sessionId.ToString());

            var session = new Session(
                id: sessionId,
                tenantId: tenantId,
                userId: userId,
                externalId: sub,
                provider: provider,
                startedAt: DateTimeOffset.UtcNow,
                expiresAt: expiresAt);



            await _sessionService.SaveSessionAsync(
                session,
                cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return sessionId;
        }

        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;

        }
    }

}