namespace FieldService.Authentication.Interfaces;

public interface ISessionPersistenceService
{
    Task InactiveCleanupAsync(CancellationToken cancellationToken = default);
}