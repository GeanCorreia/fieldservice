using FieldService.AccountManagement.Dtos;
using FieldService.Shared.Types;
using FieldService.Shared.Types.Http;

namespace FieldService.AccountManagement.Interfaces;

internal interface IAccountManagementService
{
    Task RemoveCache(Guid tenantId, Guid? entityId, CancellationToken cancellationToken);
    Task<TenantDto?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TenantReferenceDto?> GetTenantReferenceByIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IEnumerable<TenantReferenceDto>> GetTenantReferencesByTenantIdsAsync(IEnumerable<Guid> tenantIds, CancellationToken cancellationToken);
    Task<IEnumerable<TeamDto>> GetTeamsByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TeamDto?> GetTeamByIdAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken);
    Task<UserAccountDto?> GetUserByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<UserAccountDto>> GetUsersByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IEnumerable<UserAccountDto>> GetUsersByTeamIdAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken);
    Task<PaginatedResult<UserAccountDto>> GetUsersByTenantIdAsync(Guid tenantId, PagedRequest pagedRequest, CancellationToken cancellationToken);
    Task<PaginatedResult<UserAccountDto>> GetUsersByTeamIdAsync(Guid tenantId, Guid teamId, PagedRequest pagedRequest, CancellationToken cancellationToken);
    Task<IEnumerable<JobTitleDto>> GetJobTitlesByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<JobTitleDto?> GetJobTitleByIdAsync(Guid tenantId, Guid jobTitleId, CancellationToken cancellationToken);
}