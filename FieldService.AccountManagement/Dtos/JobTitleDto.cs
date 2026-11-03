using FieldService.AccountManagement.Entities;

namespace FieldService.AccountManagement.Dtos;

internal record JobTitleDto
{
    public Guid Id { get; }
    public bool IsDeactivated { get; }
    public string Title { get; }
    public string? Description { get; }
    
    public JobTitleDto(Guid id, string title, string? description = null, bool isDeactivated = false)
    {
        Id = id;
        IsDeactivated = isDeactivated;
        Title = title;
        Description = description;
    }
}

internal static class JobTitleDtoExtensions
{
    public static JobTitleDto ToDto(this JobTitle jobTitle)
    {
        return new JobTitleDto(jobTitle.Id, jobTitle.Title, jobTitle.Description, jobTitle.IsDeactivated);
    }
}