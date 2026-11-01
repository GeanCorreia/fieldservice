using FieldService.Authorization.Interfaces;
using FieldService.Shared.Types;
using MediatR;


namespace FieldService.Http.Cqrs.Queries.GetUser;

public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto?>
{
    private readonly IAuthorizationService _authorizationService;
    
    public GetUserHandler(
        IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        
    }
    
    public async Task<UserDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        return await _authorizationService.GetUserAsync(userId, cancellationToken);
    }
}