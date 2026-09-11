namespace FiledService.Audit.Interfaces;

public interface IAuditDtoSchemaBootstrapService
{
    Task EnsureSchemas(CancellationToken ct = default);
}
