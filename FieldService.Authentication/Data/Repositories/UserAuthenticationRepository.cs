using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Authentication.Data.Repositories;

internal sealed class UserAuthenticationRepository(
    AuthenticationDbContext dbContext,
    ISqlUnitOfWork<AuthenticationDbContext> unitOfWork) : IUserAuthenticationRepository
{
    public async Task Save(UserAuthentication userAuthentication, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userAuthentication);

        var existing = await dbContext.UserAuthentications
            .Include("_events")
            .FirstOrDefaultAsync(
                x => x.UserId == userAuthentication.UserId,
                ct);

        if (existing is null)
        {
            await dbContext.UserAuthentications.AddAsync(userAuthentication, ct);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(userAuthentication);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task<UserAuthentication?> GetById(Guid userId, CancellationToken ct = default)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        return await dbContext.UserAuthentications
            .Include("_events")
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);
    }

    public async Task<UserAuthentication?> GetByExternalId(
        string externalId,
        AuthenticationProvider provider,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("ExternalId is required.", nameof(externalId));
        if (!Enum.IsDefined(typeof(AuthenticationProvider), provider))
            throw new ArgumentException("Provider is required.", nameof(provider));

        return await dbContext.UserAuthentications
            .Include("_events")
            .FirstOrDefaultAsync(
                x => x.ExternalId == externalId &&
                     x.Provider == provider,
                ct);
    }
}
