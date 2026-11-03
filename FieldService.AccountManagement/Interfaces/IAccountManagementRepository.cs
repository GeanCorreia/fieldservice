using FieldService.AccountManagement.Entities;
using FieldService.Shared.Types;
using FieldService.Shared.Types.Http;

namespace FieldService.AccountManagement.Interfaces;

internal interface IAccountManagementRepository
{
    Task SaveAsync(ITenantEntity tenantEntity, CancellationToken cancellationToken);
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TenantReference?> GetTenantReferenceByIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IEnumerable<TenantReference>> GetTenantReferencesAsync(IEnumerable<Guid> tenantIds, CancellationToken cancellationToken);
    Task<IEnumerable<Team>> GetTeamsByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IEnumerable<Team>> GetTeamsByLeaderIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);
    Task<Team?> GetTeamByIdAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken);
    Task<Guid> GetDefaultTeamIdByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<bool> HasTeamAsync(Guid tenantId, string name, CancellationToken cancellationToken);
    Task<bool> HasTeamAsync(Guid tenantId, string name, Guid excludingId, CancellationToken cancellationToken);
    Task<bool> HasTeamAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken);
    Task<UserAccount?> GetUserByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<UserAccount>> GetUsersByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IEnumerable<UserAccount>> GetUsersByTeamIdAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken);
    Task<IEnumerable<UserAccount>> GetUsersByJobTitleIdAsync(Guid tenantId, Guid jobTitleId, CancellationToken cancellationToken);
    Task<PaginatedResult<UserAccount>> GetUsersByTenantIdAsync(Guid tenantId, PagedRequest pagedRequest, CancellationToken cancellationToken);
    Task<PaginatedResult<UserAccount>> GetUsersByTeamIdAsync(Guid tenantId, Guid teamId, PagedRequest pagedRequest, CancellationToken cancellationToken);
    Task<bool> HasUserAsync(Guid tenantId, Email email, CancellationToken cancellationToken);
    Task<bool> HasUserAsync(Guid tenantId, Email email, Guid excludingId, CancellationToken cancellationToken);
    Task<bool> HasUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<JobTitle>> GetJobTitlesByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<JobTitle?> GetJobTitleByIdAsync(Guid tenantId, Guid jobTitleId, CancellationToken cancellationToken);
    Task<bool> HasJobTitleAsync(Guid tenantId, string name, CancellationToken cancellationToken);
    Task<bool> HasJobTitleAsync(Guid tenantId, string name, Guid excludingId, CancellationToken cancellationToken);
    Task<bool> HasJobTitleAsync(Guid tenantId, Guid jobTitleId, CancellationToken cancellationToken);
}