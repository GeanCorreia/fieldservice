using FieldService.Data.Interfaces;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Storage.Data.Repositories;

internal sealed class StoredFileRepository(
    StorageDbContext dbContext,
    ISqlUnitOfWork<StorageDbContext> unitOfWork) : IStoredFileRepository, ICleanUpStoredFileRepository
{
    public async Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));

        return await dbContext.StoredFiles
            .Include(x => x.FileCategory)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IEnumerable<StoredFile>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var distinctIds = ids.Where(id => id != default).Distinct().ToArray();
        if (distinctIds.Length == 0)
            return Array.Empty<StoredFile>();

        return await dbContext.StoredFiles
            .Include(x => x.FileCategory)
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(ct);
    }

    public async Task SaveStoredFileAsync(StoredFile storedFile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFile);
        await SaveStoredFileCoreAsync(storedFile, ct, saveChanges: true);
    }

    public async Task SaveStoredFilesAsync(IEnumerable<StoredFile> files, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        var list = files.Where(f => f is not null).ToArray();
        if (list.Length == 0)
            return;

        foreach (var storedFile in list)
        {
            await SaveStoredFileCoreAsync(storedFile, ct, saveChanges: false);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task<IEnumerable<StoredFile>> GetByCategoryAsync(StoredFileCategory category, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        return await dbContext.StoredFiles
            .AsNoTracking()
            .Include(x => x.FileCategory)
            .Where(x => x.FileCategoryId == category.Id)
            .ToListAsync(ct);
    }

    public async Task<StoredFileCategory?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));

        return await dbContext.StoredFileCategories
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IEnumerable<StoredFileCategory>> GetCategoryByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var distinctIds = ids.Where(id => id != default).Distinct().ToArray();
        if (distinctIds.Length == 0)
            return Array.Empty<StoredFileCategory>();

        return await dbContext.StoredFileCategories
            .AsNoTracking()
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StoredFileCategory>> GetByCategoryTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (tenantId == default)
            return Array.Empty<StoredFileCategory>();

        return await dbContext.StoredFileCategories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(ct);
    }

    public async Task SaveStoredFileCategoryAsync(StoredFileCategory storedFileCategory, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFileCategory);

        var existing = await dbContext.StoredFileCategories
            .FirstOrDefaultAsync(x => x.Id == storedFileCategory.Id, ct);

        if (existing is null)
            await dbContext.StoredFileCategories.AddAsync(storedFileCategory, ct);
        else
            dbContext.Entry(existing).CurrentValues.SetValues(storedFileCategory);

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task SaveStoredFilesCategoriesAsync(IEnumerable<StoredFileCategory> categories, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(categories);

        var list = categories.Where(c => c is not null).ToArray();
        if (list.Length == 0)
            return;

        foreach (var category in list)
        {
            var existing = await dbContext.StoredFileCategories
                .FirstOrDefaultAsync(x => x.Id == category.Id, ct);

            if (existing is null)
                await dbContext.StoredFileCategories.AddAsync(category, ct);
            else
                dbContext.Entry(existing).CurrentValues.SetValues(category);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task UpdateFailedStatusAsync(Guid fileId, CancellationToken ct = default)
    {
        ValidateId(fileId, nameof(fileId));

        var file = await dbContext.StoredFiles
            .FirstOrDefaultAsync(x => x.Id == fileId, ct)
            ?? throw new KeyNotFoundException($"StoredFile '{fileId}' clean up target not found.");

        file.UpdateFailedUploadStatus();
        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task<IEnumerable<StoredFile>> GetByStatusAsync(StorageStatus status, DateTimeOffset createdBefore, CancellationToken ct = default)
    {
        return await dbContext.StoredFiles
            .Include(x => x.FileCategory)
            .Where(x => x.Status == status && x.UploadedAt < createdBefore)
            .ToListAsync(ct);
    }

    private async Task SaveStoredFileCoreAsync(
        StoredFile storedFile,
        CancellationToken ct,
        bool saveChanges)
    {
        // 1. Busca sem Include para performance e evita rastrear entidades desnecessárias
        var existing = await dbContext.StoredFiles
            .FirstOrDefaultAsync(x => x.Id == storedFile.Id, ct);

        // 2. Resolve a categoria vinda de outro escopo
        AttachOrReuseCategory(storedFile);

        if (existing is null)
        {
            await dbContext.StoredFiles.AddAsync(storedFile, ct);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(storedFile);
            dbContext.Entry(existing).Property(x => x.FileCategoryId).CurrentValue = storedFile.FileCategoryId;
        }

        if (saveChanges)
            await unitOfWork.PersistChangesAsync(ct);
    }

    private void AttachOrReuseCategory(StoredFile storedFile)
    {
        if (storedFile.FileCategory is null) return;
        
        var trackedCategory = dbContext.ChangeTracker.Entries<StoredFileCategory>()
            .FirstOrDefault(x => x.Entity.Id == storedFile.FileCategory.Id || x.Entity.Id == storedFile.FileCategoryId)?.Entity;

        if (trackedCategory is not null)
        {
            dbContext.Entry(storedFile).Reference(x => x.FileCategory).CurrentValue = trackedCategory;
        }
        else
        {
            dbContext.Attach(storedFile.FileCategory);
        }
    }

    private static void ValidateId(Guid id, string paramName)
    {
        if (id == default)
            throw new ArgumentException("Id cannot be empty.", paramName);
    }
}