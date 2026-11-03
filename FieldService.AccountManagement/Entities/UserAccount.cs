using FieldService.AccountManagement.Events;
using FieldService.AccountManagement.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Entities;

internal class UserAccount : ITenantEntity
{
    // Required by EF materialization.
    protected UserAccount()
    {
        Events = new List<TenantEventBase>();
    }

    public UserAccount(ICollection<TenantEventBase> events)
    {
        Events = events ?? throw new ArgumentNullException(nameof(events));
        ValidateEventsConsistency();
    }

    public ICollection<TenantEventBase> Events { get; private set; }
    public Guid Id => GetRegisteredEvent().UserId;
    public Guid TenantId => GetRegisteredEvent().TenantId;
    public Guid TeamId => ResolveTeamId();
    public string TeamName => Events
        .OfType<TeamNameUpdatedEvent>()
        .Where(e => e.TeamId == TeamId)
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.Name)
        .FirstOrDefault()
        ?? Events
            .OfType<TeamRegisteredEvent>()
            .Where(e => e.TeamId == TeamId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Name)
            .FirstOrDefault()
        ?? throw new InvalidOperationException("Team name could not be determined from events.");

    public PersonalInfo PersonalInfo => BuildPersonalInfo();
    public JobTitle? JobTitle => BuildJobTitle();
    public bool IsDeactivate()
    {
        var latestLifecycleEvent = Events
            .Select((tenantEvent, index) => new { Event = tenantEvent, Index = index })
            .Where(item => item.Event is UserAccountDeactivatedEvent or UserAccountReactivatedEvent)
            .OrderByDescending(item => item.Event.CreatedAt)
            .ThenByDescending(item => item.Index)
            .Select(item => item.Event)
            .FirstOrDefault();

        return latestLifecycleEvent is UserAccountDeactivatedEvent;
    }

    public void Deactivate(Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (IsDeactivate())
            return;

        Events.Add(UserAccountDeactivatedEvent.Create(Id, TenantId, createdBy, createdAt));
    }

    public void Reactivate(Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (!IsDeactivate())
            return;

        Events.Add(UserAccountReactivatedEvent.Create(Id, TenantId, createdBy, createdAt));
    }

    public static UserAccount Create(
        Guid userId,
        Guid tenantId,
        Guid teamId,
        Guid jobTitleId,
        string fullName,
        string displayName,
        Email email,
        Phone phone,
        Guid createdBy,
        DateTimeOffset? createdAt = null)
    {
        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;

        var events = new List<TenantEventBase>
        {
            UserAccountRegisteredEvent.Create(userId, tenantId, teamId, jobTitleId, createdBy, effectiveCreatedAt),
            PersonalInfoRegisteredEvent.Create(userId, tenantId, fullName, displayName, email, phone, createdBy, effectiveCreatedAt)
        };

        
        var userAccount = new UserAccount();
        userAccount.Events = events;
        return userAccount;
    }

    public void UpdateFullName(string fullName, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("FullName is required.", nameof(fullName));

        var normalizedFullName = fullName.Trim();
        if (string.Equals(PersonalInfo.FullName, normalizedFullName, StringComparison.Ordinal))
            return;

        Events.Add(PersonalInfoFullNameUpdatedEvent.Create(
            Id,
            TenantId,
            normalizedFullName,
            createdBy,
            createdAt));
    }

    public void UpdateDisplayName(string displayName, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName is required.", nameof(displayName));

        var normalizedDisplayName = displayName.Trim();
        if (string.Equals(PersonalInfo.DisplayName, normalizedDisplayName, StringComparison.Ordinal))
            return;

        Events.Add(PersonalInfoDisplayNameUpdatedEvent.Create(
            Id,
            TenantId,
            normalizedDisplayName,
            createdBy,
            createdAt));
    }

    public void UpdateEmail(Email email, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (PersonalInfo.Email == email)
            return;

        Events.Add(PersonalInfoEmailUpdatedEvent.Create(
            Id,
            TenantId,
            email,
            createdBy,
            createdAt));
    }

    public void UpdatePhone(Phone phone, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (PersonalInfo.Phone == phone)
            return;

        Events.Add(PersonalInfoPhoneUpdatedEvent.Create(
            Id,
            TenantId,
            phone,
            createdBy,
            createdAt));
    }
    
    public void UpdateTeam(Guid teamId, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (teamId == default)
            throw new ArgumentException("TeamId is required.", nameof(teamId));

        if (TeamId == teamId)
            return;

        Events.Add(UserTeamTransferredEvent.Create(
            Id,
            teamId,
            TenantId,
            createdBy,
            createdAt));
    }
    public void UpdateJobTitle(Guid jobTitleId, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (jobTitleId == default)
            throw new ArgumentException("JobTitleId is required.", nameof(jobTitleId));

        if (ResolveJobTitleId() == jobTitleId)
            return;

        Events.Add(UserJobTitleChangedEvent.Create(
            Id,
            jobTitleId,
            TenantId,
            createdBy,
            createdAt));
    }

    private UserAccountRegisteredEvent GetRegisteredEvent()
    {
        return Events
            .OfType<UserAccountRegisteredEvent>()
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("User account registration event is required.");
    }

    private Guid ResolveTeamId()
    {
        var transferredTeamId = Events
            .Where(e => e is UserTeamTransferredEvent transfer && transfer.UserId == Id && transfer.TenantId == TenantId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => ((UserTeamTransferredEvent)e).TeamId)
            .FirstOrDefault();

        if (transferredTeamId != Guid.Empty)
            return transferredTeamId;

        return Events
            .OfType<UserAccountRegisteredEvent>()
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.TeamId)
            .FirstOrDefault();
    }

    private PersonalInfo BuildPersonalInfo()
    {
        var personalInfoEvents = Events
            .Where(e => e is PersonalInfoRegisteredEvent registered && registered.UserId == Id
                || e is PersonalInfoFullNameUpdatedEvent fullName && fullName.UserId == Id
                || e is PersonalInfoDisplayNameUpdatedEvent displayName && displayName.UserId == Id
                || e is PersonalInfoEmailUpdatedEvent email && email.UserId == Id
                || e is PersonalInfoPhoneUpdatedEvent phone && phone.UserId == Id)
            .OrderBy(e => e.CreatedAt)
            .ToList();

        return new PersonalInfo(personalInfoEvents);
    }

    private JobTitle? BuildJobTitle()
    {
        var jobTitleId = ResolveJobTitleId();
        var jobTitleEvents = Events
            .Where(e => e is JobTitleRegisteredEvent title && title.JobTitleId == jobTitleId
                || e is JobTitleTitleUpdatedEvent titleUpdated && titleUpdated.JobTitleId == jobTitleId
                || e is JobTitleDescriptionUpdatedEvent description && description.JobTitleId == jobTitleId
                || e is JobTitleDeactivatedEvent deactivated && deactivated.JobTitleId == jobTitleId)
            .OrderBy(e => e.CreatedAt)
            .ToList();
        if (jobTitleEvents.Count == 0)
        {
            return null;
        }
        return new JobTitle(jobTitleEvents);
    }

    private Guid? ResolveJobTitleId()
    {
        return Events
            .OfType<UserJobTitleChangedEvent>()
            .Where(e => e.UserId == Id && e.TenantId == TenantId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => (Guid?)e.JobTitleId)
            .FirstOrDefault()
            ?? GetRegisteredEvent().JobTitleId;
    }

    private void ValidateEventsConsistency()
    {
        var registeredEvent = GetRegisteredEvent();

        var personalInfoOrigins = Events
            .Select(e => e switch
            {
                PersonalInfoRegisteredEvent registered => (HasValue: true, UserId: registered.UserId, TenantId: registered.TenantId),
                PersonalInfoFullNameUpdatedEvent fullName => (HasValue: true, UserId: fullName.UserId, TenantId: fullName.TenantId),
                PersonalInfoDisplayNameUpdatedEvent displayName => (HasValue: true, UserId: displayName.UserId, TenantId: displayName.TenantId),
                PersonalInfoEmailUpdatedEvent email => (HasValue: true, UserId: email.UserId, TenantId: email.TenantId),
                PersonalInfoPhoneUpdatedEvent phone => (HasValue: true, UserId: phone.UserId, TenantId: phone.TenantId),
                _ => (HasValue: false, UserId: Guid.Empty, TenantId: Guid.Empty)
            })
            .Where(origin => origin.HasValue)
            .ToList();

        if (personalInfoOrigins.Count == 0)
            throw new InvalidOperationException("At least one personal info event is required.");

        var personalInfoRegisteredEvents = Events.OfType<PersonalInfoRegisteredEvent>().ToList();
        if (personalInfoRegisteredEvents.Count == 0)
            throw new InvalidOperationException("Personal info registered event is required.");

        var invalidPersonalInfoOrigin = personalInfoOrigins.FirstOrDefault(origin =>
            origin.UserId != registeredEvent.UserId ||
            origin.TenantId != registeredEvent.TenantId);

        if (invalidPersonalInfoOrigin.HasValue)
            throw new InvalidOperationException("All personal info events must belong to the same User and Tenant as the registration event.");

        var teamTransfers = Events.OfType<UserTeamTransferredEvent>().ToList();
        if (teamTransfers.Any(e =>
                e.UserId != registeredEvent.UserId || e.TenantId != registeredEvent.TenantId))
            throw new InvalidOperationException("All team transfer events must belong to the same User and Tenant as the registration event.");

        var lifecycleEvents = Events
            .Where(e => e is UserAccountDeactivatedEvent or UserAccountReactivatedEvent)
            .ToList();
        if (lifecycleEvents.Any(e =>
                e.TenantId != registeredEvent.TenantId ||
                (e is UserAccountDeactivatedEvent deactivated && deactivated.UserId != registeredEvent.UserId) ||
                (e is UserAccountReactivatedEvent reactivated && reactivated.UserId != registeredEvent.UserId)))
            throw new InvalidOperationException("All user lifecycle events must belong to the same User and Tenant as the registration event.");

        var assignedTeamIds = teamTransfers
            .Select(e => e.TeamId)
            .Append(registeredEvent.TeamId)
            .ToHashSet();

        var teamOrigins = Events
            .Select(e => e switch
            {
                TeamRegisteredEvent registered => (HasValue: true, TeamId: registered.TeamId, TenantId: registered.TenantId),
                TeamNameUpdatedEvent name => (HasValue: true, TeamId: name.TeamId, TenantId: name.TenantId),
                _ => (HasValue: false, TeamId: Guid.Empty, TenantId: Guid.Empty)
            })
            .Where(origin => origin.HasValue)
            .ToList();

        if (teamOrigins.Count == 0)
            throw new InvalidOperationException("At least one team event is required.");

        var teamRegisteredEvents = Events.OfType<TeamRegisteredEvent>().ToList();
        if (teamRegisteredEvents.Count == 0)
            throw new InvalidOperationException("Team registered event is required.");

        var invalidTeamOrigin = teamOrigins.FirstOrDefault(origin =>
            !assignedTeamIds.Contains(origin.TeamId) ||
            origin.TenantId != registeredEvent.TenantId);

        if (invalidTeamOrigin.HasValue)
            throw new InvalidOperationException("All team events must belong to an assigned Team and the same Tenant as the registration event.");

        var jobTitleChanges = Events.OfType<UserJobTitleChangedEvent>().ToList();
        if (jobTitleChanges.Any(e =>
                e.UserId != registeredEvent.UserId || e.TenantId != registeredEvent.TenantId))
            throw new InvalidOperationException("All job title change events must belong to the same User and Tenant as the registration event.");

        var assignedJobTitleIds = jobTitleChanges
            .Select(e => (Guid?)e.JobTitleId)
            .Append(registeredEvent.JobTitleId)
            .ToHashSet();

        var jobTitleOrigins = Events
            .Select(e => e switch
            {
                JobTitleRegisteredEvent title => (HasValue: true, JobTitleId: title.JobTitleId, TenantId: title.TenantId),
                JobTitleTitleUpdatedEvent titleUpdated => (HasValue: true, JobTitleId: titleUpdated.JobTitleId, TenantId: titleUpdated.TenantId),
                JobTitleDescriptionUpdatedEvent description => (HasValue: true, JobTitleId: description.JobTitleId, TenantId: description.TenantId),
                _ => (HasValue: false, JobTitleId: Guid.Empty, TenantId: Guid.Empty)
            })
            .Where(origin => origin.HasValue)
            .ToList();

        if (jobTitleOrigins.Count > 0)
        {
            var invalidJobTitleOrigin = jobTitleOrigins.FirstOrDefault(origin =>
                !assignedJobTitleIds.Contains(origin.JobTitleId) ||
                origin.TenantId != registeredEvent.TenantId);

            if (invalidJobTitleOrigin.HasValue)
                throw new InvalidOperationException("All job title events must belong to an assigned JobTitle and the same Tenant as the registration event.");
        }
    }
}