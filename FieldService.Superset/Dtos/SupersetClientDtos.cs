using FieldService.Superset.Interfaces;

namespace FieldService.Superset.Dtos;



internal enum SupersetResourceType
{
    Dashboard = 1,  
    Chart = 2,      
    Dataset = 3,    
    Database = 4   
}


public record SupersetTokenResponse(
    string Token);