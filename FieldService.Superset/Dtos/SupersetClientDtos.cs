using FieldService.Superset.Interfaces;

namespace FieldService.Superset.Dtos;



internal enum SupersetResourceType
{
    Dashboard = 1,  
    Chart = 2,      
    Dataset = 3,    
    Database = 4   
}

internal record SupersetClientRequest(
    string ResourceId,
    SupersetResourceType ResourceType);
    
public record SupersetTokenResponse(
    string Token);