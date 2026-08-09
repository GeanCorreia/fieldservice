using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Mappers;

internal sealed class SessionMapper : ISessionMapper
{
    private readonly IUserAuthenticationMapper  _userAuthenticationMapper;

    public SessionMapper(IUserAuthenticationMapper userAuthenticationMapper)
    {
        _userAuthenticationMapper = userAuthenticationMapper ?? throw new ArgumentNullException(
            nameof(userAuthenticationMapper));
    }
    public SessionCacheModel Map(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new SessionCacheModel(
            session.Id,
            session.UserId,
            session.TenantId,
            session.ExternalId,
            session.Provider,
            session.StartedAt,
            session.ExpiresAt,
            session.RevokedAt,
            session.RevocationReason,
            session.LastActivityAt,
            session.Activities.Select(Map).ToArray());
    }

    public Session Map(SessionCacheModel cacheModel)
    {
        ArgumentNullException.ThrowIfNull(cacheModel);

        var activities = cacheModel.Activities.Select(Map).OfType<SessionActivity>().ToArray();



        return new Session(
            activities!,
            cacheModel.Id,
            cacheModel.UserId,
            cacheModel.TenantId,
            cacheModel.ExternalId,
            cacheModel.Provider,
            cacheModel.StartedAt,
            cacheModel.ExpiresAt,
            cacheModel.RevokedAt,
            cacheModel.RevocationReason,
            cacheModel.LastActivityAt);
    }

    public SessionActivityCacheModel Map(SessionActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        return new SessionActivityCacheModel(
            activity.Id,
            activity.SessionId,
            activity.JwtId,
            activity.IpAddressHash,
            activity.UserAgentHash,
            activity.Timestamp,
            activity.Channel,
            activity.RequestId);
    }

    public SessionActivity? Map(SessionActivityCacheModel? cacheModel)
    {
        if (cacheModel is null)
            return null;

        return new SessionActivity(
            cacheModel.Id,
            cacheModel.SessionId,
            cacheModel.JwtId,
            cacheModel.IpAddressHash,
            cacheModel.Timestamp,
            cacheModel.Channel,
            cacheModel.RequestId,
            cacheModel.UserAgentHash);
    }
}
