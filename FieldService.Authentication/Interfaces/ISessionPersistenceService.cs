namespace FieldService.Authentication.Interfaces;

public interface ISessionPersistenceService
{
    Task PersistAsync(CancellationToken cancellationToken = default);
}