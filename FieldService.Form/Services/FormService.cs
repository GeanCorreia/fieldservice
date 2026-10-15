using FieldService.Data.Interfaces;
using FieldService.Form.Data.Repositories;
using FieldService.Form.Dtos;
using FieldService.Form.Enums;
using FieldService.Form.Interfaces;
using FieldService.Form.Messages;
using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.SignalR.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace FieldService.Form.Services;

internal sealed class FormService(
    
    HybridCache cache,
    FormRepository repository,
    IMessageSender messageSender,
    IUnitOfWork unitOfWork,
    IMediator mediator) : IFormService
{
    private const string FormPrefix = "form:";
    private const string FormDtoByIdPrefix = FormPrefix + "id:";
    private const string FormIdsByTenantPrefix = FormPrefix + "tenant:";


    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMessageSender _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
    private readonly HybridCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly FormRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

    private HybridCacheEntryOptions CacheOptions => new()
    {
        Expiration = _cacheTtl,
        LocalCacheExpiration = _cacheTtl
    };
    
   
    public async Task<FormDto?> GetFormDtoAsync(
        Guid formId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (formId == default)
            throw new ArgumentException("FormId is required.", nameof(formId));

        var form = await _cache.GetOrCreateAsync<FormDto?>(
            GetFormDtoByIdKey(formId),
            async ct =>
            {
                var form = await _repository.GetFormAsync(formId, ct);
                return form?.ToDto();
            },
            CacheOptions,
            cancellationToken: cancellationToken);
        
        if (form is null)
        {
            throw new KeyNotFoundException($"Form '{formId}' not found.");
        }
        
        if (form.TenantId != tenantId)
        {
            throw new KeyNotFoundException($"Form '{formId}' not found for tenant '{tenantId}'.");
        }

        return form;
    }

    public async Task<IEnumerable<FormDto>> GetFormsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        var formIds = await GetFormIdsByTenantAsync(tenantId, cancellationToken);
        if (formIds.Count == 0)
            return [];

        var forms = await Task.WhenAll(
            formIds.Select(formId => GetFormDtoAsync(formId, tenantId, cancellationToken)));

        return forms.Where(form => form is not null)!;
    }

    public async Task<IEnumerable<FormInfoDto>> GetFormInfosByTenantAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        var forms = await GetFormsAsync(tenantId, cancellationToken);
        
        forms = forms.Where(f => f.IsActive);
        
        return forms.Select(form => new FormInfoDto(form.Id, form.Title, form.Description)).ToList();
    }
    
    private FormMessage CreateFormMessage(
        FormOperation operation,
        FormDto form)
    {
        var messagePayload = new FormMessagePayload(
            form.TenantId,
            operation,
            new FormInfoDto(form.Id, form.Title, form.Description)
        );
            
        return new FormMessage(messagePayload);
    }

    public async Task CreateFormAsync(
        FormDto form,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _mediator.Send(new GetUserTenantQuery(userId, form.TenantId), cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }
        
        var domainForm = Entities.Form.Create( 
            tenantId: form.TenantId,
            userId: userId,
            title: form.Title,
            description: form.Description,
            schemaJson: form.SchemaJson,
            id: form.Id);
        
        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            await _repository.SaveFormAsync(domainForm, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        
        try
        {
 
            await _cache.SetAsync(
                GetFormDtoByIdKey(form.Id), 
                form, 
                CacheOptions, 
                cancellationToken: cancellationToken);
            
            await InvalidateFormCacheAsync(
                tenantId: form.TenantId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            //No error handling here, as we don't want to fail the save operation if cache invalidation fails.
        }

        try
        {
            var message = CreateFormMessage(FormOperation.Create, form);
            await _messageSender.SendTenantAsync(
                form.TenantId,
                message);
        }
        catch
        {
            //No error handling here, as we don't want to fail the save operation if message sending fails.
        }
    }

    

    public async Task DeleteFormAsync(
        Guid formId, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        var form = await _repository.GetFormAsync(formId, cancellationToken);
        if(form == null)
            throw new KeyNotFoundException($"Form with id '{formId}' not found.");
        
        var user = await _mediator.Send(new GetUserTenantQuery(userId, form.TenantId), cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }
        form.DeleteForm(userId);
        await _repository.SaveFormAsync(form, cancellationToken);
        
        try
        {
            await InvalidateFormCacheAsync(
                formId: formId,
                tenantId: form.TenantId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            //No error handling here, as we don't want to fail the save operation if cache invalidation fails.
        }
        
        var formDto = form.ToDto();
        
        try
        {

            
            var message = CreateFormMessage(FormOperation.Delete, formDto);
            await _messageSender.SendTenantAsync(
                form.TenantId,
                message);
        }
        catch
        {
            //No error handling here, as we don't want to fail the save operation if message sending fails.
        }
        
    }

    public async Task UpdateFormAsync(
        FormDto form, 
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserTenantQuery(userId, form.TenantId), cancellationToken);
        
        var domainForm = await _repository.GetFormAsync(form.Id, cancellationToken);
        
        if (domainForm is null)
            throw new KeyNotFoundException($"Form with id '{form.Id}' not found.");
        
        

        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }

        var updates = new Dictionary<string, object>();

        if (form.Title != domainForm.Title)
        {
            updates.Add(nameof(form.Title), form.Title);
        }
        
        if (form.Description != domainForm.Description)
        {
            updates.Add(nameof(form.Description), form.Description);
        }
        
        if(form.SchemaJson.ToString() != domainForm.SchemaJson.ToString())
        {
            updates.Add(nameof(form.SchemaJson), form.SchemaJson);
        }
        
        if (updates.Count == 0)
            return;
        
        domainForm.UpdateForm(
            userId: userId,
            title: form.Title,
            description: form.Description,
            schemaJson: form.SchemaJson);  
        
        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            await _repository.SaveFormAsync(domainForm, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        try
        {
            await InvalidateFormCacheAsync(
                formId: form.Id,
                cancellationToken: cancellationToken);
        }
        catch
        {
            //No error handling here, as we don't want to fail the save operation if cache invalidation fails.
        }

        try
        {

            
            var message = CreateFormMessage(FormOperation.Update, form);
            await _messageSender.SendTenantAsync(
                form.TenantId,
                message);
        }
        catch
        {
            //No error handling here, as we don't want to fail the save operation if message sending fails.
        }
        
    }

    private async Task<IReadOnlyList<Guid>> GetFormIdsByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync<IReadOnlyList<Guid>>(
            GetFormIdsByTenantKey(tenantId),
            async ct =>
            {
                var forms = await _repository.GetFormsAsync(tenantId, ct);
                var formDtos = forms
                    .Where(f => f.IsActive)
                    .Select(f => f.ToDto())
                    .ToList();

                // Warm up `form:id:{id}` cache while creating the tenant id index.
                foreach (var formDto in formDtos)
                {
                    await _cache.SetAsync(
                        GetFormDtoByIdKey(formDto.Id),
                        formDto,
                        CacheOptions,
                        cancellationToken: ct);
                }

                return formDtos.Select(formDto => formDto.Id).ToList();
            },
            CacheOptions,
            cancellationToken: cancellationToken);
    }


    private async Task InvalidateFormCacheAsync(
        Guid? formId = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if(formId.HasValue)
        {
            await _cache.RemoveAsync(GetFormDtoByIdKey(formId.Value), cancellationToken);
            
        }
       
        if(tenantId.HasValue)
        {
            await _cache.RemoveAsync(GetFormIdsByTenantKey(tenantId.Value), cancellationToken);
            
        }
        
    }

    private static string GetFormDtoByIdKey(Guid formId) => $"{FormDtoByIdPrefix}{formId}";
    private static string GetFormIdsByTenantKey(Guid tenantId) => $"{FormIdsByTenantPrefix}{tenantId}";

    

}