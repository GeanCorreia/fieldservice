using FieldService.AccountManagement.Entities;

namespace FieldService.AccountManagement.Dtos;

internal record TeamDto
{
    public Guid Id { get; }
    public bool IsDeactivated { get; }
    public string Name { get; }
    public string? Description { get; }
    public Guid? LeaderId { get; }
    public string? LeaderName { get; init; } 

    public TeamDto(Guid id, string name, string? description = null, Guid? leaderId = null, string? leaderName = null, bool isDeactivated = false)
    {
        Id = id;
        IsDeactivated = isDeactivated;
        Name = name;
        Description = description;
        LeaderId = leaderId;
        LeaderName = leaderName;
    }
}

internal static class TeamDtoExtensions
{
    public static TeamDto ToDto(this Team team)
    {
        return new TeamDto(team.Id, team.Name, team.Description, team.LeaderId, team.LeaderName, team.IsDeactivated);
    }
}
