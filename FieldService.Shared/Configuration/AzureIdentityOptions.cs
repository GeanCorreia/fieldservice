using System.ComponentModel.DataAnnotations;
namespace FieldService.Shared.Configuration;


public class AzureIdentityOptions
{
    public const string SectionName = "AzureIdentity";

    [Required]
    public string AzureSubscriptionId { get; set; } = string.Empty;

    [Required]
    public string AzureResourceGroupName { get; set; } = string.Empty;

    [Required]
    public string AzureLocation { get; set; } = "eastus";

    [Required]
    public string AzureContainerAppEnvironmentId { get; set; } = string.Empty;
    
}