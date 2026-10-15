using System.Text.Json;
using FieldService.Data.Interfaces;
using FieldService.Form.Dtos;
using FieldService.Form.Entities;
using FieldService.Form.Extensions;
using FieldService.Form.Interfaces;
using FieldService.Shared.Types.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FieldService.Form.Data.Repositories;

internal sealed class FormSubmissionRepository(
    FormDataDbContext dbContext,
    ISqlUnitOfWork<FormDataDbContext> unitOfWork) : IFormSubmissionRepository
{
    public async Task<FormSubmission?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id, nameof(id));

        return await dbContext.FormSubmissions
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDelete, cancellationToken);
    }

    public async Task<FormSubmission?> GetByIdIncludingDeletedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id, nameof(id));

        return await dbContext.FormSubmissions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task SaveAsync(
        FormSubmission formSubmission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(formSubmission);

        var existing = await dbContext.FormSubmissions
            .FirstOrDefaultAsync(x => x.Id == formSubmission.Id, cancellationToken);

        if (existing is null)
        {
            await dbContext.FormSubmissions.AddAsync(formSubmission, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(formSubmission);
        }

        await unitOfWork.PersistChangesAsync(cancellationToken);
    }

    public async Task<PaginatedResult<FormSubmission>> GetByFilterAsync(
        Guid formId,
        IEnumerable<FormFieldData> filters,
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default)
    {
        ValidateId(formId, nameof(formId));
        ArgumentNullException.ThrowIfNull(filters);

        var normalizedPagination = NormalizePagination(paginationRequestDto);
        var filteredQuery = BuildFilteredQuery(formId, filters);

        var totalCount = await filteredQuery.LongCountAsync(cancellationToken);
        var totalPages = CalculateTotalPages(totalCount, normalizedPagination.PageSize);
        var page = ClampPage(normalizedPagination.Page, totalPages);

        var orderedQuery = normalizedPagination.IsDescending
            ? filteredQuery.OrderByDescending(x => x.LastModifiedAt)
            : filteredQuery.OrderBy(x => x.LastModifiedAt);

        var items = await orderedQuery
            .Skip((page - 1) * normalizedPagination.PageSize)
            .Take(normalizedPagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<FormSubmission>(
            new Pagination(page, normalizedPagination.PageSize, totalCount, totalPages),
            items);
    }

    public async Task<PaginatedResult<FormSubmission>> GetAllAsync(
        Guid formId,
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default)
    {
        ValidateId(formId, nameof(formId));

        var normalizedPagination = NormalizePagination(paginationRequestDto);

        var query = dbContext.FormSubmissions
            .AsNoTracking()
            .Where(x => x.FormId == formId && !x.IsDelete);

        var totalCount = await query.LongCountAsync(cancellationToken);
        var totalPages = CalculateTotalPages(totalCount, normalizedPagination.PageSize);
        var page = ClampPage(normalizedPagination.Page, totalPages);

        var orderedQuery = normalizedPagination.IsDescending
            ? query.OrderByDescending(x => x.LastModifiedAt)
            : query.OrderBy(x => x.LastModifiedAt);

        var items = await orderedQuery
            .Skip((page - 1) * normalizedPagination.PageSize)
            .Take(normalizedPagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<FormSubmission>(
            new Pagination(page, normalizedPagination.PageSize, totalCount, totalPages),
            items);
    }

    public async Task<PaginatedResult<FormSubmission>> GetAllDeletedAsync(
        Guid formId, 
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default)
    {
        ValidateId(formId, nameof(formId));

        var normalizedPagination = NormalizePagination(paginationRequestDto);
        
        var query = dbContext.FormSubmissions
            .AsNoTracking()
            .Where(x => x.FormId == formId && x.IsDelete);
        
        var totalCount = await query.LongCountAsync(cancellationToken);
        var totalPages = CalculateTotalPages(totalCount, normalizedPagination.PageSize);
        var page = ClampPage(normalizedPagination.Page, totalPages);

        var orderedQuery = normalizedPagination.IsDescending
            ? query.OrderByDescending(x => x.LastModifiedAt)
            : query.OrderBy(x => x.LastModifiedAt);

        var items = await orderedQuery
            .Skip((page - 1) * normalizedPagination.PageSize)
            .Take(normalizedPagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<FormSubmission>(
            new Pagination(page, normalizedPagination.PageSize, totalCount, totalPages),
            items);
    }

    private IQueryable<FormSubmission> BuildFilteredQuery(
        Guid formId,
        IEnumerable<FormFieldData> filters)
    {
        var activeFilters = filters
            .Where(f => !string.IsNullOrWhiteSpace(f.FieldName))
            .ToArray();

        if (activeFilters.Length == 0)
        {
            return dbContext.FormSubmissions
                .AsNoTracking()
                .Where(x => x.FormId == formId && !x.IsDelete);
        }

        var sql = new System.Text.StringBuilder(
            $"SELECT * FROM \"{FormTenant.DatabaseSchema}\".\"{nameof(FormSubmission)}\" WHERE \"FormId\" = @formId AND NOT \"IsDelete\"");

        var parameters = new List<object>
        {
            new NpgsqlParameter<Guid>("formId", formId)
        };

        for (var i = 0; i < activeFilters.Length; i++)
        {
            var filter = activeFilters[i];
            var fieldName = filter.FieldName.Trim();
            if (fieldName.Length == 0)
                continue;

            var pathSegments = fieldName.ToPathSegments();
            var pathExpression = pathSegments.ToPostgresTextArrayExpression();
            var valueParameterName = $"value{i}";
            
            if(filter.Value == null)
                continue;
            
            var expectedText = GetComparableText(filter.Value);
            if (string.IsNullOrWhiteSpace(expectedText))
                continue;

            sql.Append($" AND (\"DataJson\" #>> {pathExpression}) ");
            if (IsStringLike(filter.Value))
            {
                
                sql.Append($"ILIKE ('%' || @{valueParameterName} || '%')");
            }
            else
            {
                sql.Append($"= @{valueParameterName}");
            }

            parameters.Add(new NpgsqlParameter<string>(valueParameterName, expectedText));
        }

        return dbContext.FormSubmissions
            .FromSqlRaw(sql.ToString(), parameters.ToArray())
            .AsNoTracking();
    }

    private static PagedRequest NormalizePagination(
        PagedRequest? paginationRequestDto)
    {
        var page = paginationRequestDto?.Page > 0 ? paginationRequestDto.Page : 1;
        var pageSize = paginationRequestDto?.PageSize > 0 ? paginationRequestDto.PageSize : 20;
        var isDescending = paginationRequestDto?.IsDescending ?? false;

        return new PagedRequest(
            page, 
            pageSize, 
            IsDescending: isDescending);
    }

    private static int CalculateTotalPages(long totalCount, int pageSize)
    {
        if (totalCount == 0)
            return 0;

        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    private static int ClampPage(int page, int totalPages)
    {
        if (totalPages == 0)
            return 1;

        if (page > totalPages)
            return totalPages;

        return page > 0 ? page : 1;
    }

    private static void ValidateId(Guid id, string paramName)
    {
        if (id == default)
            throw new ArgumentException("Id cannot be empty.", paramName);
    }

    private static bool IsStringLike(object value)
        => value is string
           || (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String);

    private static string? GetComparableText(object value)
        => value switch
        {
            string text => text.Trim(),
            JsonElement jsonElement => jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString()?.Trim(),
                JsonValueKind.Null => null,
                _ => jsonElement.ToString().Trim()
            },
            _ => Convert.ToString(value)?.Trim()
        };

    public async ValueTask DisposeAsync()
    {
        switch (unitOfWork)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }

        await dbContext.DisposeAsync();
    }
}
