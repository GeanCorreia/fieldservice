namespace FieldService.Superset.Attributes;

/// <summary>
/// Define a lista de permissões (PVM - Permission View Menu) do Flask-AppBuilder/Superset 
/// que devem ser REVOGADAS/CANCELADAS dos perfis Alpha e SQL_Lab no tenant.
/// </summary>
public static class SupersetRestrictedPermissions
{
    public readonly record struct PermissionView(string Permission, string ViewMenu);

    /// <summary>
    /// Permissões de Conexão com Banco de Dados e Infraestrutura.
    /// Revoga o acesso às Connection Strings, configurações e gerenciamento do PostgreSQL do Tenant.
    /// </summary>
    public static readonly IReadOnlyList<PermissionView> DatabaseAndConnectionRestrictions = new PermissionView[]
    {
        new("can_read", "Database"),
        new("can_write", "Database"),
        new("can_add", "Database"),
        new("can_delete", "Database"),
        new("can_export", "Database"), // Impede exportar a CONFIGURAÇÃO do banco (YAML), NÃO os dados em CSV
        new("can_external_metadata_by_name", "Database"),
        new("can_select_star", "Database"),
        new("can_external_metadata", "Datasource"),
        new("can_external_metadata_by_name", "Datasource")
    };

    /// <summary>
    /// Permissões de Modificação e Criação de Estruturas/Datasets.
    /// Bloqueia a criação/alteração de novas tabelas, views ou colunas salvas no Superset, 
    /// mas PRESERVA a execução de SELECTs e a exportação do resultado em .CSV.
    /// </summary>
    public static readonly IReadOnlyList<PermissionView> SchemaAndTableCreationRestrictions = new PermissionView[]
    {
        new("can_get_or_create_dataset", "Dataset"),
        new("can_save", "Datasource"),
        new("can_add", "Dataset"),
        new("can_write", "Dataset"),
        new("can_delete", "Dataset"),
        new("can_refresh", "Dataset"),
    };

    /// <summary>
    /// Conjunto consolidado das permissões a serem revogadas dos papéis 'Alpha' e 'sql_lab'.
    /// </summary>
    public static readonly IReadOnlyList<PermissionView> AllTenantRestrictions = 
        DatabaseAndConnectionRestrictions
            .Concat(SchemaAndTableCreationRestrictions)
            .ToList()
            .AsReadOnly();
}

