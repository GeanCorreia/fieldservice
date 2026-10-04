namespace FieldService.Superset.Dtos;

internal record SupersetRole(
    int Id,
    string Name,
    List<string> Permissions);
 