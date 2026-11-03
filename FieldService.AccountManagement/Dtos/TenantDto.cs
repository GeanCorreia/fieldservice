using FieldService.AccountManagement.Entities;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos;

internal record TenantDto(
    TenantReferenceDto Tenant,
    IEnumerable<JobTitleDto> JobTitles,
    IEnumerable<TeamDto> Teams
    );
    

internal static class  TenantDtoExtensions  
{
    public static TenantDto ToTenantDto(this Tenant tenant)
    {
        return new TenantDto(
            tenant.ToTenantReference(),
            tenant.JobTitles.Select(jt => jt.ToDto()),
            tenant.Teams.Select(t => t.ToDto())
        );
    }

    public static TenantReferenceDto ToTenantReference(this Tenant tenant)
    {
        return new TenantReferenceDto(tenant.TenantId, tenant.Reference.Name);
    }
}