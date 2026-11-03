using FieldService.AccountManagement.Data;
using FieldService.AccountManagement.Entities;
using FieldService.AccountManagement.Events;
using FieldService.AccountManagement.Interfaces;
using FieldService.Data.Interfaces;
using FieldService.Shared.Types;
using FieldService.Shared.Types.Http;
using Microsoft.EntityFrameworkCore;

namespace FieldService.AccountManagement.Data.Repositories;

/// <summary>
/// Repositório event-sourced: todas as entidades são reconstruídas a partir dos eventos
/// da tabela única TenantEvents. Cada leitura executa UMA única consulta, filtrando por
/// TenantId + tipo de evento (+ TeamId/JobTitleId/UserId), coberta pelos índices da tabela.
/// </summary>
/// <remarks>
/// As leituras são rastreadas pelo ChangeTracker. <see cref="SaveAsync"/> marca os
/// eventos novos como <c>Added</c> e solicita a persistência pela Unit of Work.
/// Uma transação explícita ativa continua dependendo do commit de seu coordenador.
/// </remarks>
internal sealed class AccountManagementRepository(
    AccountManagementDbContext dbContext,
    ISqlUnitOfWork<AccountManagementDbContext> unitOfWork) : IAccountManagementRepository
{
    private static readonly TenantEventType[] TenantAggregateTypes =
    [
        TenantEventType.TenantCreated,
        TenantEventType.TenantNameUpdated,
        TenantEventType.TeamRegistered,
        TenantEventType.TeamNameUpdated,
        TenantEventType.TeamDescriptionUpdated,
        TenantEventType.TeamLeaderUpdated,
        TenantEventType.TeamDeactivated,
        TenantEventType.JobTitleRegistered,
        TenantEventType.JobTitleTitleUpdated,
        TenantEventType.JobTitleDescriptionUpdated,
        TenantEventType.JobTitleDeactivated
    ];

    private static readonly TenantEventType[] TenantReferenceTypes =
    [
        TenantEventType.TenantCreated,
        TenantEventType.TenantNameUpdated
    ];

    private static readonly TenantEventType[] TeamTypes =
    [
        TenantEventType.TeamRegistered,
        TenantEventType.TeamNameUpdated,
        TenantEventType.TeamDescriptionUpdated,
        TenantEventType.TeamLeaderUpdated,
        TenantEventType.TeamDeactivated
    ];

    private static readonly TenantEventType[] JobTitleTypes =
    [
        TenantEventType.JobTitleRegistered,
        TenantEventType.JobTitleTitleUpdated,
        TenantEventType.JobTitleDescriptionUpdated,
        TenantEventType.JobTitleDeactivated
    ];

    // Eventos necessários para montar UserAccount (usuário + dados pessoais + nome do time + cargo).
    private static readonly TenantEventType[] UserGraphTypes =
    [
        TenantEventType.UserAccountRegistered,
        TenantEventType.UserAccountDeactivated,
        TenantEventType.UserAccountReactivated,
        TenantEventType.UserTeamTransferred,
        TenantEventType.UserJobTitleChanged,
        TenantEventType.PersonalInfoRegistered,
        TenantEventType.PersonalInfoFullNameUpdated,
        TenantEventType.PersonalInfoDisplayNameUpdated,
        TenantEventType.PersonalInfoEmailUpdated,
        TenantEventType.PersonalInfoPhoneUpdated,
        TenantEventType.TeamRegistered,
        TenantEventType.TeamNameUpdated,
        TenantEventType.JobTitleRegistered,
        TenantEventType.JobTitleTitleUpdated,
        TenantEventType.JobTitleDescriptionUpdated
    ];

    public Task SaveAsync(ITenantEntity tenantEntity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenantEntity);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var tenantEvent in tenantEntity.Events)
        {
            var entry = dbContext.Entry(tenantEvent);
            if (entry.State == EntityState.Detached)
                entry.State = EntityState.Added;
        }

        return unitOfWork.PersistChangesAsync(cancellationToken);
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantEvents = dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId && TenantAggregateTypes.Contains(e.EventType));
        var teamEvents = dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId && TeamTypes.Contains(e.EventType));

        var events = await tenantEvents
            .Concat(BuildTeamLeaderQuery(tenantId, teamEvents))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return events.OfType<TenantCreatedEvent>().Any() ? new Tenant(events) : null;
    }

    public async Task<TenantReference?> GetTenantReferenceByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var events = await dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId && TenantReferenceTypes.Contains(e.EventType))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return events.OfType<TenantCreatedEvent>().Any() ? new TenantReference(events) : null;
    }

    public async Task<IEnumerable<TenantReference>> GetTenantReferencesAsync(IEnumerable<Guid> tenantIds, CancellationToken cancellationToken)
    {
        var tenantIdSet = tenantIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (tenantIdSet.Length == 0)
            return [];

        var events = await dbContext.TenantEvents
            .Where(e => tenantIdSet.Contains(e.TenantId) && TenantReferenceTypes.Contains(e.EventType))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return events
            .GroupBy(e => e.TenantId)
            .Where(g => g.OfType<TenantCreatedEvent>().Any())
            .Select(g => new TenantReference(g.ToList()))
            .ToList();
    }

    public async Task<IEnumerable<Team>> GetTeamsByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var teamEvents = dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId && TeamTypes.Contains(e.EventType));

        var events = await teamEvents
            .Concat(BuildTeamLeaderQuery(tenantId, teamEvents))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return Team.BuildTeams(events);
    }

    public async Task<IEnumerable<Team>> GetTeamsByLeaderIdAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var teams = await GetTeamsByTenantIdAsync(tenantId, cancellationToken);
        return teams.Where(team => team.LeaderId == userId).ToList();
    }

    public async Task<Team?> GetTeamByIdAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken)
    {
        var teamEventsQuery = dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId && (
                (e is TeamRegisteredEvent && ((TeamRegisteredEvent)e).TeamId == teamId) ||
                (e is TeamNameUpdatedEvent && ((TeamNameUpdatedEvent)e).TeamId == teamId) ||
                (e is TeamDescriptionUpdatedEvent && ((TeamDescriptionUpdatedEvent)e).TeamId == teamId) ||
                (e is TeamLeaderUpdatedEvent && ((TeamLeaderUpdatedEvent)e).TeamId == teamId) ||
                (e is TeamDeactivatedEvent && ((TeamDeactivatedEvent)e).TeamId == teamId)));

        var events = await teamEventsQuery
            .Concat(BuildTeamLeaderQuery(tenantId, teamEventsQuery))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var deduplicatedEvents = events
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .OrderBy(e => e.CreatedAt)
            .ToList();

        return Team.BuildTeams(deduplicatedEvents).FirstOrDefault();
    }

    public async Task<Guid> GetDefaultTeamIdByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId &&
                        e is TeamRegisteredEvent &&
                        ((TeamRegisteredEvent)e).Name == Team.DefaultTeamName)
            .Select(e => ((TeamRegisteredEvent)e).TeamId)
            .FirstAsync(cancellationToken);
    }

    public Task<bool> HasTeamAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken)
    {
        return dbContext.TenantEvents
            .OfType<TeamRegisteredEvent>()
            .AnyAsync(e => e.TenantId == tenantId && e.TeamId == teamId, cancellationToken);
    }

    public Task<bool> HasTeamAsync(Guid tenantId, string name, CancellationToken cancellationToken)
    {
        return HasTeamAsync(tenantId, name, Guid.Empty, cancellationToken);
    }

    public async Task<bool> HasTeamAsync(
        Guid tenantId, string name, Guid excludingId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedName = name.Trim();

        return await dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId &&
                (e is TeamRegisteredEvent || e is TeamNameUpdatedEvent))
            .Select(e => new
            {
                TeamId = e is TeamRegisteredEvent
                    ? ((TeamRegisteredEvent)e).TeamId
                    : ((TeamNameUpdatedEvent)e).TeamId,
                Name = e is TeamRegisteredEvent
                    ? ((TeamRegisteredEvent)e).Name
                    : ((TeamNameUpdatedEvent)e).Name,
                e.CreatedAt
            })
            .Where(e => e.TeamId != excludingId)
            .GroupBy(e => e.TeamId)
            .Select(group => group.OrderByDescending(e => e.CreatedAt)
                .Select(e => e.Name)
                .First())
            .AnyAsync(currentName => currentName == normalizedName, cancellationToken);
    }

    public async Task<UserAccount?> GetUserByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var registered = dbContext.TenantEvents
            .OfType<UserAccountRegisteredEvent>()
            .Where(r => r.TenantId == tenantId && r.UserId == userId);

        var events = await BuildUserGraphQuery(tenantId, registered)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return BuildUsers(events).FirstOrDefault();
    }

    public async Task<IEnumerable<UserAccount>> GetUsersByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var events = await dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId && UserGraphTypes.Contains(e.EventType))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return BuildUsers(events);
    }

    public async Task<IEnumerable<UserAccount>> GetUsersByTeamIdAsync(Guid tenantId, Guid teamId, CancellationToken cancellationToken)
    {
        var registered = GetRegisteredUsersInTeam(tenantId, teamId);

        var events = await BuildUserGraphQuery(tenantId, registered)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return BuildUsers(events);
    }

    public async Task<IEnumerable<UserAccount>> GetUsersByJobTitleIdAsync(
        Guid tenantId, Guid jobTitleId, CancellationToken cancellationToken)
    {
        var registered = GetRegisteredUsersWithJobTitle(tenantId, jobTitleId);
        var events = await BuildUserGraphQuery(tenantId, registered)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return BuildUsers(events);
    }

    public Task<PaginatedResult<UserAccount>> GetUsersByTenantIdAsync(
        Guid tenantId, PagedRequest pagedRequest, CancellationToken cancellationToken)
    {
        var registered = dbContext.TenantEvents
            .OfType<UserAccountRegisteredEvent>()
            .Where(e => e.TenantId == tenantId);

        return GetUsersPageAsync(tenantId, registered, pagedRequest, cancellationToken);
    }

    public Task<PaginatedResult<UserAccount>> GetUsersByTeamIdAsync(
        Guid tenantId, Guid teamId, PagedRequest pagedRequest, CancellationToken cancellationToken)
    {
        return GetUsersPageAsync(
            tenantId, GetRegisteredUsersInTeam(tenantId, teamId), pagedRequest, cancellationToken);
    }

    private async Task<PaginatedResult<UserAccount>> GetUsersPageAsync(
        Guid tenantId,
        IQueryable<UserAccountRegisteredEvent> registered,
        PagedRequest pagedRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pagedRequest);
        var request = new PagedRequest(
            pagedRequest.Page, pagedRequest.PageSize, pagedRequest.SortBy, pagedRequest.IsDescending);
        var ordered = OrderUsers(registered, request);
        var skip = checked((request.Page - 1) * request.PageSize);
        var totalRecords = await registered.LongCountAsync(cancellationToken);
        var pagination = new Pagination(
            request.Page, request.PageSize, totalRecords,
            checked((int)(totalRecords / request.PageSize + (totalRecords % request.PageSize == 0 ? 0 : 1))));

        if (skip >= totalRecords)
            return new PaginatedResult<UserAccount>(pagination, []);

        var userIds = await ordered.Skip(skip).Take(request.PageSize)
            .Select(e => e.UserId)
            .ToListAsync(cancellationToken);
        var page = dbContext.TenantEvents.OfType<UserAccountRegisteredEvent>()
            .Where(e => e.TenantId == tenantId && userIds.Contains(e.UserId));
        var events = await BuildUserGraphQuery(tenantId, page)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
        var usersById = BuildUsers(events).ToDictionary(user => user.Id);
        var users = userIds.Select(id => usersById[id]).ToList();

        return new PaginatedResult<UserAccount>(pagination, users);
    }

    private static IOrderedQueryable<UserAccountRegisteredEvent> OrderUsers(
        IQueryable<UserAccountRegisteredEvent> registered, PagedRequest request)
    {
        return request.SortBy?.Trim().ToLowerInvariant() switch
        {
            null or "" or "createdat" => request.IsDescending
                ? registered.OrderByDescending(e => e.CreatedAt).ThenBy(e => e.UserId)
                : registered.OrderBy(e => e.CreatedAt).ThenBy(e => e.UserId),
            "id" or "userid" => request.IsDescending
                ? registered.OrderByDescending(e => e.UserId)
                : registered.OrderBy(e => e.UserId),
            _ => throw new ArgumentException(
                "Supported user sort fields are CreatedAt, Id and UserId.", nameof(request))
        };
    }

    public Task<bool> HasUserAsync(Guid tenantId, Email email, CancellationToken cancellationToken)
    {
        return HasUserAsync(tenantId, email, Guid.Empty, cancellationToken);
    }

    public async Task<bool> HasUserAsync(
        Guid tenantId, Email email, Guid excludingId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        return await dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId &&
                (e is PersonalInfoRegisteredEvent || e is PersonalInfoEmailUpdatedEvent))
            .Select(e => new
            {
                UserId = e is PersonalInfoRegisteredEvent
                    ? ((PersonalInfoRegisteredEvent)e).UserId
                    : ((PersonalInfoEmailUpdatedEvent)e).UserId,
                Email = e is PersonalInfoRegisteredEvent
                    ? ((PersonalInfoRegisteredEvent)e).Email
                    : ((PersonalInfoEmailUpdatedEvent)e).Email,
                e.CreatedAt
            })
            .Where(e => e.UserId != excludingId)
            .GroupBy(e => e.UserId)
            .Select(group => group.OrderByDescending(e => e.CreatedAt)
                .Select(e => e.Email)
                .First())
            .AnyAsync(currentEmail => currentEmail == email, cancellationToken);
    }

    public async Task<bool> HasUserAsync(
        Guid tenantId, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        return await dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId && (e is UserAccountRegisteredEvent))
            .AnyAsync(e => ((UserAccountRegisteredEvent)e).UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<JobTitle>> GetJobTitlesByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var events = await dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId && JobTitleTypes.Contains(e.EventType))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return events
            .GroupBy(GetJobTitleId)
            .Select(g => new JobTitle(g.ToList()))
            .ToList();
    }

    public async Task<JobTitle?> GetJobTitleByIdAsync(Guid tenantId, Guid jobTitleId, CancellationToken cancellationToken)
    {
        var events = await dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId && (
                (e is JobTitleRegisteredEvent && ((JobTitleRegisteredEvent)e).JobTitleId == jobTitleId) ||
                (e is JobTitleTitleUpdatedEvent && ((JobTitleTitleUpdatedEvent)e).JobTitleId == jobTitleId) ||
                (e is JobTitleDescriptionUpdatedEvent && ((JobTitleDescriptionUpdatedEvent)e).JobTitleId == jobTitleId) ||
                (e is JobTitleDeactivatedEvent && ((JobTitleDeactivatedEvent)e).JobTitleId == jobTitleId)))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return events.OfType<JobTitleRegisteredEvent>().Any() ? new JobTitle(events) : null;
    }

    public Task<bool> HasJobTitleAsync(Guid tenantId, Guid jobTitleId, CancellationToken cancellationToken)
    {
        return dbContext.TenantEvents
            .OfType<JobTitleRegisteredEvent>()
            .AnyAsync(e => e.TenantId == tenantId && e.JobTitleId == jobTitleId, cancellationToken);
    }

    public Task<bool> HasJobTitleAsync(Guid tenantId, string name, CancellationToken cancellationToken)
    {
        return HasJobTitleAsync(tenantId, name, Guid.Empty, cancellationToken);
    }

    public async Task<bool> HasJobTitleAsync(
        Guid tenantId, string name, Guid excludingId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedName = name.Trim();

        return await dbContext.TenantEvents
            .Where(e => e.TenantId == tenantId &&
                (e is JobTitleRegisteredEvent || e is JobTitleTitleUpdatedEvent))
            .Select(e => new
            {
                JobTitleId = e is JobTitleRegisteredEvent
                    ? ((JobTitleRegisteredEvent)e).JobTitleId
                    : ((JobTitleTitleUpdatedEvent)e).JobTitleId,
                Title = e is JobTitleRegisteredEvent
                    ? ((JobTitleRegisteredEvent)e).Title
                    : ((JobTitleTitleUpdatedEvent)e).Title,
                e.CreatedAt
            })
            .Where(e => e.JobTitleId != excludingId)
            .GroupBy(e => e.JobTitleId)
            .Select(group => group.OrderByDescending(e => e.CreatedAt)
                .Select(e => e.Title)
                .First())
            .AnyAsync(currentTitle => currentTitle == normalizedName, cancellationToken);
    }

    private IQueryable<TenantEventBase> BuildTeamLeaderQuery(
        Guid tenantId,
        IQueryable<TenantEventBase> teamEvents)
    {
        var leaderUpdates = teamEvents.OfType<TeamLeaderUpdatedEvent>();
        var leaderIds = teamEvents.OfType<TeamRegisteredEvent>()
            .Select(r => leaderUpdates
                .Where(e => e.TeamId == r.TeamId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => e.LeaderId)
                .FirstOrDefault());
        var initialLeaderIds = teamEvents.OfType<TeamRegisteredEvent>()
            .Where(r => !leaderUpdates.Any(e => e.TeamId == r.TeamId))
            .Select(r => r.LeaderId);
        var currentLeaderIds = leaderIds.Concat(initialLeaderIds);

        return dbContext.TenantEvents.Where(e => e.TenantId == tenantId && (
            (e is PersonalInfoRegisteredEvent &&
                currentLeaderIds.Contains(((PersonalInfoRegisteredEvent)e).UserId)) ||
            (e is PersonalInfoDisplayNameUpdatedEvent &&
                currentLeaderIds.Contains(((PersonalInfoDisplayNameUpdatedEvent)e).UserId))));
    }

    private IQueryable<UserAccountRegisteredEvent> GetRegisteredUsersInTeam(Guid tenantId, Guid teamId)
    {
        var transfers = dbContext.TenantEvents
            .OfType<UserTeamTransferredEvent>()
            .Where(e => e.TenantId == tenantId);

        return dbContext.TenantEvents
            .OfType<UserAccountRegisteredEvent>()
            .Where(r => r.TenantId == tenantId &&
                (transfers.Where(e => e.UserId == r.UserId)
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => (Guid?)e.TeamId)
                    .FirstOrDefault() ?? r.TeamId) == teamId);
    }

    private IQueryable<UserAccountRegisteredEvent> GetRegisteredUsersWithJobTitle(Guid tenantId, Guid jobTitleId)
    {
        var jobTitleChanges = dbContext.TenantEvents
            .OfType<UserJobTitleChangedEvent>()
            .Where(e => e.TenantId == tenantId);

        return dbContext.TenantEvents
            .OfType<UserAccountRegisteredEvent>()
            .Where(r => r.TenantId == tenantId &&
                (jobTitleChanges.Where(e => e.UserId == r.UserId)
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => (Guid?)e.JobTitleId)
                    .FirstOrDefault() ?? r.JobTitleId) == jobTitleId);
    }

    private IQueryable<TenantEventBase> BuildUserGraphQuery(
        Guid tenantId,
        IQueryable<UserAccountRegisteredEvent> registered)
    {
        var transfers = dbContext.TenantEvents
            .OfType<UserTeamTransferredEvent>()
            .Where(e => e.TenantId == tenantId && registered.Any(r => r.UserId == e.UserId));
        var jobTitleChanges = dbContext.TenantEvents
            .OfType<UserJobTitleChangedEvent>()
            .Where(e => e.TenantId == tenantId && registered.Any(r => r.UserId == e.UserId));
        var teamIds = registered.Select(r => transfers.Where(e => e.UserId == r.UserId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => (Guid?)e.TeamId)
            .FirstOrDefault() ?? r.TeamId);
        var jobTitleIds = registered.Select(r => r.JobTitleId)
            .Concat(jobTitleChanges.Select(e => (Guid?)e.JobTitleId));

        return dbContext.TenantEvents.Where(e => e.TenantId == tenantId && (
            (e is UserAccountRegisteredEvent && registered.Any(r => r.Id == e.Id)) ||
            (e is UserAccountDeactivatedEvent && registered.Any(r => r.UserId == ((UserAccountDeactivatedEvent)e).UserId)) ||
            (e is UserAccountReactivatedEvent && registered.Any(r => r.UserId == ((UserAccountReactivatedEvent)e).UserId)) ||
            (e is UserTeamTransferredEvent && registered.Any(r => r.UserId == ((UserTeamTransferredEvent)e).UserId)) ||
            (e is UserJobTitleChangedEvent && registered.Any(r => r.UserId == ((UserJobTitleChangedEvent)e).UserId)) ||
            (e is PersonalInfoRegisteredEvent && registered.Any(r => r.UserId == ((PersonalInfoRegisteredEvent)e).UserId)) ||
            (e is PersonalInfoFullNameUpdatedEvent && registered.Any(r => r.UserId == ((PersonalInfoFullNameUpdatedEvent)e).UserId)) ||
            (e is PersonalInfoDisplayNameUpdatedEvent && registered.Any(r => r.UserId == ((PersonalInfoDisplayNameUpdatedEvent)e).UserId)) ||
            (e is PersonalInfoEmailUpdatedEvent && registered.Any(r => r.UserId == ((PersonalInfoEmailUpdatedEvent)e).UserId)) ||
            (e is PersonalInfoPhoneUpdatedEvent && registered.Any(r => r.UserId == ((PersonalInfoPhoneUpdatedEvent)e).UserId)) ||
            (e is TeamRegisteredEvent && teamIds.Contains(((TeamRegisteredEvent)e).TeamId)) ||
            (e is TeamNameUpdatedEvent && teamIds.Contains(((TeamNameUpdatedEvent)e).TeamId)) ||
            (e is JobTitleRegisteredEvent && jobTitleIds.Contains(((JobTitleRegisteredEvent)e).JobTitleId)) ||
            (e is JobTitleTitleUpdatedEvent && jobTitleIds.Contains(((JobTitleTitleUpdatedEvent)e).JobTitleId)) ||
            (e is JobTitleDescriptionUpdatedEvent && jobTitleIds.Contains(((JobTitleDescriptionUpdatedEvent)e).JobTitleId)) ||
            (e is JobTitleDeactivatedEvent && jobTitleIds.Contains(((JobTitleDeactivatedEvent)e).JobTitleId))));
    }

    private static List<UserAccount> BuildUsers(IReadOnlyCollection<TenantEventBase> events)
    {
        var transfersByUser = events.OfType<UserTeamTransferredEvent>().ToLookup(e => e.UserId);
        var jobTitleChangesByUser = events.OfType<UserJobTitleChangedEvent>().ToLookup(e => e.UserId);
        var lifecycleEventsByUser = events
            .Where(e => e is UserAccountDeactivatedEvent or UserAccountReactivatedEvent)
            .ToLookup(e => e is UserAccountDeactivatedEvent deactivated
                ? deactivated.UserId
                : ((UserAccountReactivatedEvent)e).UserId);
        var personalByUser = events
            .Select(e => (UserId: GetPersonalInfoUserId(e), Event: e))
            .Where(x => x.UserId.HasValue)
            .ToLookup(x => x.UserId!.Value, x => x.Event);

        var teamByTeam = events
            .Where(e => e is TeamRegisteredEvent or TeamNameUpdatedEvent)
            .ToLookup(GetTeamId);

        var jobTitleByJobTitle = events
            .Where(e => e is JobTitleRegisteredEvent or JobTitleTitleUpdatedEvent
                or JobTitleDescriptionUpdatedEvent or JobTitleDeactivatedEvent)
            .ToLookup(GetJobTitleId);

        return events
            .OfType<UserAccountRegisteredEvent>()
            .Select(r =>
            {
                var transfers = transfersByUser[r.UserId].ToList();
                var jobTitleChanges = jobTitleChangesByUser[r.UserId].ToList();
                var teamId = transfers.OrderByDescending(e => e.CreatedAt)
                    .Select(e => (Guid?)e.TeamId).FirstOrDefault() ?? r.TeamId;
                var teamEvents = teamByTeam[teamId];
                var jobTitleEvents = jobTitleChanges.Select(e => (Guid?)e.JobTitleId)
                    .Append(r.JobTitleId).Where(id => id.HasValue).Distinct()
                    .SelectMany(id => jobTitleByJobTitle[id!.Value]);

                return new UserAccount(
                    new TenantEventBase[] { r }
                        .Concat(personalByUser[r.UserId])
                        .Concat(lifecycleEventsByUser[r.UserId])
                        .Concat(transfers)
                        .Concat(jobTitleChanges)
                        .Concat(teamEvents)
                        .Concat(jobTitleEvents)
                        .OrderBy(e => e.CreatedAt)
                        .ToList());
            })
            .ToList();
    }

    private static Guid? GetPersonalInfoUserId(TenantEventBase e) => e switch
    {
        PersonalInfoRegisteredEvent x => x.UserId,
        PersonalInfoFullNameUpdatedEvent x => x.UserId,
        PersonalInfoDisplayNameUpdatedEvent x => x.UserId,
        PersonalInfoEmailUpdatedEvent x => x.UserId,
        PersonalInfoPhoneUpdatedEvent x => x.UserId,
        _ => null
    };

    private static Guid GetTeamId(TenantEventBase e) => e switch
    {
        TeamRegisteredEvent x => x.TeamId,
        TeamNameUpdatedEvent x => x.TeamId,
        TeamDescriptionUpdatedEvent x => x.TeamId,
        TeamLeaderUpdatedEvent x => x.TeamId,
        TeamDeactivatedEvent x => x.TeamId,
        _ => throw new InvalidOperationException($"Event {e.GetType().Name} is not a team event.")
    };

    private static Guid GetJobTitleId(TenantEventBase e) => e switch
    {
        JobTitleRegisteredEvent x => x.JobTitleId,
        JobTitleTitleUpdatedEvent x => x.JobTitleId,
        JobTitleDescriptionUpdatedEvent x => x.JobTitleId,
        JobTitleDeactivatedEvent x => x.JobTitleId,
        _ => throw new InvalidOperationException($"Event {e.GetType().Name} is not a job title event.")
    };
}
