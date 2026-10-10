using System.ComponentModel.DataAnnotations.Schema;

namespace FieldService.Form.Entities;


internal class FormTenant
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid ConnectionStringId { get; set; }
    public bool DeveloperMcpEnabled { get; set; } 
    public bool FormExecutionEnabled { get; set; }
    
    [NotMapped]
    public string SecretName => FormSecretName(TenantId);

    public FormTenant(
        Guid id, 
        Guid tenantId, 
        Guid connectionStringId,
        bool developerMcpEnabled,
        bool formExecutionEnabled)
    {
        Id = id;
        TenantId = tenantId;
        ConnectionStringId = connectionStringId;
        DeveloperMcpEnabled = developerMcpEnabled;
        FormExecutionEnabled = formExecutionEnabled;
    }
    public static string FormSecretName(Guid tenantId) 
    {
        return $"form-tenant-secret-{tenantId:N}";
    }

    public static string DatabaseSchema => "form";
    public static string DatabaseSchemaName => DatabaseSchema;
    
    public static string DatabaseName(Guid tenantId) => $"db_tenant_{tenantId:N}";
    

}