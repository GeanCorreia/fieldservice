using FieldService.Superset.Attributes;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Entities;

namespace FieldService.Superset.Utils;

public static class SupersetQueryApiResolver
{
   public static SupersetFilter OwnerUsernameFilter(string username)
    {
        return new SupersetFilter("owners.username", "rel_m_m", username);
    }
    
    public static SupersetFilter OwnerFilter(Guid userId, Guid tenantId)
    {
        var userName = SupersetUsernameResolver.ResolveUsername(userId, tenantId);
        return OwnerUsernameFilter(userName);
    }
    
    public static SupersetFilter SupersetDatasetNameFilter(string databaseName)
    {
        return new SupersetFilter("database", "eq", databaseName);
    }
    
    public static SupersetFilter SupersetDatasetFilter(Guid tenantId)
    {
        var databaseName = SupersetTenant.Database(tenantId);
        return SupersetDatasetNameFilter(databaseName);
    }
}