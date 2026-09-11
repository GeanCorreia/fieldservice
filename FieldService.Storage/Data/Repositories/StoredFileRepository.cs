using FieldService.Data.Interfaces;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Storage.Data.Repositories;

internal sealed class StoredFileRepository(
    StorageDbContext dbContext,
    ISqlUnitOfWork<StorageDbContext> unitOfWork) : IStoredFileRepository
{
    public async Task<StoredFile?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        ValidateFileId(id);

        return await dbContext.StoredFiles
            .AsNoTracking()
            .Include(x => x.FileCategory)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IEnumerable<StoredFile>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var distinctIds = ids.Where(id => id != default).Distinct().ToArray();
        if (distinctIds.Length == 0)
            return Array.Empty<StoredFile>();

        return await dbContext.StoredFiles
            .AsNoTracking()
            .Include(x => x.FileCategory)
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(ct);
    }

    public async Task SaveStoredFileAsync(
        StoredFile storedFile,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFile);

        var existing = await dbContext.StoredFiles
            .Include(x => x.FileCategory)
            .FirstOrDefaultAsync(x => x.Id == storedFile.Id, ct);

        if (existing is null)
        {
            await AttachCategoryAsync(storedFile.FileCategory, ct);
            await dbContext.StoredFiles.AddAsync(storedFile, ct);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(storedFile);
            dbContext.Entry(existing).Reference(x => x.FileCategory).CurrentValue = storedFile.FileCategory;
            dbContext.Entry(existing).Property(x => x.FileCategoryId).CurrentValue = storedFile.FileCategoryId;
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task SaveStoredFilesAsync(
        IEnumerable<StoredFile> files,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        var list = files.ToArray();
        if (list.Length == 0)
            return;

        foreach (var storedFile in list)
        {
            ArgumentNullException.ThrowIfNull(storedFile);
            await SaveStoredFileCoreAsync(storedFile, ct, saveChanges: false);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task<IEnumerable<StoredFile>> GetByCategoryAsync(
        StoredFileCategory category,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        return await dbContext.StoredFiles
            .AsNoTracking()
            .Include(x => x.FileCategory)
            .Where(x => x.FileCategoryId == category.Id)
            .ToListAsync(ct);
    }

    public async Task<StoredFileCategory?> GetCategoryByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        ValidateCategoryId(id);

        return await dbContext.StoredFileCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IEnumerable<StoredFileCategory>> GetCategoryByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken ct = default)
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

    public async Task<IEnumerable<StoredFileCategory>> GetByCategoryTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        if (tenantId == default)
            return Array.Empty<StoredFileCategory>();

        return await dbContext.StoredFileCategories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(ct);
    }

    public async Task SaveStoredFileCategoryAsync(
        StoredFileCategory storedFileCategory,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFileCategory);

        var existing = await dbContext.StoredFileCategories.FirstOrDefaultAsync(x => x.Id == storedFileCategory.Id, ct);
        if (existing is null)
        {
            await dbContext.StoredFileCategories.AddAsync(storedFileCategory, ct);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(storedFileCategory);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task SaveStoredFilesCategoriesAsync(
        IEnumerable<StoredFileCategory> categories,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(categories);

        var list = categories.ToArray();
        if (list.Length == 0)
            return;

        foreach (var category in list)
        {
            if (category is null)
                continue;

            var existing = await dbContext.StoredFileCategories
                .FirstOrDefaultAsync(x => x.Id == category.Id, ct);

            if (existing is null)
                await dbContext.StoredFileCategories.AddAsync(category, ct);
            else
                dbContext.Entry(existing).CurrentValues.SetValues(category);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task UpdateUploadedStatusAsync(
        Guid fileId,
        CancellationToken ct = default)
    {
        ValidateFileId(fileId);

        var file = await dbContext.StoredFiles
            .Include(x => x.FileCategory)
            .FirstOrDefaultAsync(x => x.Id == fileId, ct)
            ?? throw new KeyNotFoundException($"File '{fileId}' was not found.");

        file.UpdateUploadedStatus();
        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task UpdateFailedStatusAsync(
        Guid fileId,
        CancellationToken ct = default)
    {
        ValidateFileId(fileId);

        var file = await dbContext.StoredFiles
            .Include(x => x.FileCategory)
            .FirstOrDefaultAsync(x => x.Id == fileId, ct)
            ?? throw new KeyNotFoundException($"File '{fileId}' was not found.");

        file.UpdateFailedStatus();
        await unitOfWork.PersistChangesAsync(ct);
    }

    private async Task SaveStoredFileCoreAsync(
        StoredFile storedFile,
        CancellationToken ct,
        bool saveChanges = true)
    {
        var existing = await dbContext.StoredFiles
            .Include(x => x.FileCategory)
            .FirstOrDefaultAsync(x => x.Id == storedFile.Id, ct);

        if (existing is null)
        {
            await AttachCategoryAsync(storedFile.FileCategory, ct);
            await dbContext.StoredFiles.AddAsync(storedFile, ct);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(storedFile);
            dbContext.Entry(existing).Reference(x => x.FileCategory).CurrentValue = storedFile.FileCategory;
            dbContext.Entry(existing).Property(x => x.FileCategoryId).CurrentValue = storedFile.FileCategoryId;
        }

        if (saveChanges)
            await unitOfWork.PersistChangesAsync(ct);
    }

    private async Task AttachCategoryAsync(StoredFileCategory category, CancellationToken ct)
    {
        var tracked = dbContext.ChangeTracker.Entries<StoredFileCategory>()
            .FirstOrDefault(x => x.Entity.Id == category.Id)?.Entity;

        if (tracked is not null)
            return;

        var existing = await dbContext.StoredFileCategories.FirstOrDefaultAsync(x => x.Id == category.Id, ct);
        if (existing is not null)
        {
            dbContext.Attach(existing);
            return;
        }

        dbContext.Attach(category);
    }

    private static void ValidateFileId(Guid fileId)
    {
        if (fileId == default)
            throw new ArgumentException("FileId is required.", nameof(fileId));
    }

    private static void ValidateCategoryId(Guid categoryId)
    {
        if (categoryId == default)
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));
    }
}


