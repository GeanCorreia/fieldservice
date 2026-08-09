using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

public interface ISessionMapper
{
    SessionCacheModel Map(Session session);
    Session Map(SessionCacheModel cacheModel);
    SessionActivityCacheModel Map(SessionActivity activity);
    SessionActivity? Map(SessionActivityCacheModel? cacheModel);
}
