using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Controllers;

public class AccountManagementApiVersion : IApiVersion
{
    public SchemaVersion SchemaVersion { get; } = new SchemaVersion(1, 1, 4);
}