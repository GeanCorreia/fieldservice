using System.IO.Compression;
using FieldService.Data.Interfaces;
using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Shared.Configuration;
using FieldService.Shared.Message;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Types;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Storage;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Jobs;

internal record SupersetTenantMigrationJobPayload
(
    Guid MigrationId,
    Guid TenantId,
    SupersetTenantCreateParams CreateParams
) : AbstractMessagePayload<SupersetTenantMigrationJobPayload>;
    

internal record ExportSupersetTenantMigrationFileJob : Job<SupersetTenantMigrationJobPayload>
{
    public static readonly JobType JobType = "superset-tenant-extract-migration-file-job";

    internal ExportSupersetTenantMigrationFileJob(SupersetTenantMigrationJobPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.TenantId))
    {
    }
}
    
internal class ExportSupersetTenantMigrationFileJobHandler : IQueueConsumer<SupersetTenantMigrationJobPayload>
{
    private readonly ISupersetTenantInstanceProcessingLock _processingLock;
    private readonly ISupersetResourceServices _supersetResourceServices;
    private readonly ISupersetTenantFlowRepository _repository;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly IStorageService _storageService;
    private readonly ApplicationAccountOptions _applicationAccountOptions;
    private readonly IMediator _mediator;
    private readonly IStoredFileService _storedFileService;
    private readonly IUnitOfWork _unitOfWork;


    
    public ExportSupersetTenantMigrationFileJobHandler(
        IMediator mediator,
        ISupersetResourceServices supersetResourceServices,
        ISupersetTenantInstanceProcessingLock processingLock,
        ISupersetTenantFlowRepository repository, 
        ISupersetAuthService supersetAuthService, 
        ISupersetApi supersetApi,
        ISupersetTenantService supersetTenantService,
        IStorageService storageService,
        IOptions<ApplicationAccountOptions> applicationAccount,
        IStoredFileService storedFileService,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _applicationAccountOptions = applicationAccount.Value ?? throw new ArgumentNullException(nameof(applicationAccount));
        _supersetResourceServices = supersetResourceServices ?? throw new ArgumentNullException(nameof(supersetResourceServices));
        _storedFileService = storedFileService ?? throw new ArgumentNullException(nameof(storedFileService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _processingLock = processingLock ?? throw new ArgumentNullException(nameof(processingLock));
    }

    public async Task ExecuteAsync(Job<SupersetTenantMigrationJobPayload> job, CancellationToken ct = default)
    {
        if (job.Context.Type != ExportSupersetTenantMigrationFileJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");
        
        var tenantMigration = await _repository.GetByIdAsync(job.Payload.MigrationId, ct);
        if (tenantMigration == null)
        {
            throw new InvalidOperationException($"No Superset tenant migration process found for tenant {job.Payload.TenantId}.");
        }
        
        if(tenantMigration.FlowType != FlowType.Migration)
        {
            throw new InvalidOperationException($"Superset tenant migration process for tenant {job.Payload.TenantId} is not a migration process.");
        }

        if (tenantMigration.TenantId != job.Payload.TenantId)
        {
            throw new InvalidOperationException($"Tenant ID mismatch for migration process. Expected: {tenantMigration.TenantId}, Actual: {job.Payload.TenantId}.");
            
        }
        
        if(!await _processingLock.AcquireLock(job.Payload.TenantId, ct))
        {
            throw new InvalidOperationException($"Failed to acquire processing lock for tenant {job.Payload.TenantId}.");
        }
        
        
        try
        {
            var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(job.Payload.TenantId, ct);
            if (supersetTenant == null)
            {
                throw new SupersetTenantNotFoundException(job.Payload.TenantId);
            }

            var resources = await _supersetResourceServices.GetSupersetTenantResourcesAdminAsync(
                job.Payload.TenantId,
                ct);

            var dashboardIds = resources.Dashboards
                .Select(d => int.TryParse(d.Resource.ResourceId, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            var standAloneChartIds = resources.Charts
                .Select(c => int.TryParse(c.Resource.ResourceId, out var id) ? id : (int?)null)
                .Where(id => id.HasValue && !dashboardIds.Contains(id.Value))
                .Select(id => id!.Value)
                .ToList();

            var savedQueryIds = resources.SavedQueries
                .Select(sq => int.TryParse(sq.Resource.ResourceId, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            var host = new Uri(supersetTenant.FqdnUrl);

            var adminToken = await _supersetAuthService.GetAdminToken(job.Payload.TenantId, ct);
            if (string.IsNullOrWhiteSpace(adminToken))
            {
                throw new InvalidOperationException("Superset admin token not found.");
            }

            var bearerToken = $"Bearer {adminToken}";
            var exportTasks = new List<Task<HttpResponseMessage>>();

            if (dashboardIds.Any())
            {
                var qParam = $"[{string.Join(",", dashboardIds)}]";
                exportTasks.Add(_supersetApi.ExportDashboardsAsync(host, bearerToken, qParam, ct));
            }

            if (standAloneChartIds.Any())
            {
                var qParam = $"[{string.Join(",", standAloneChartIds)}]";
                exportTasks.Add(_supersetApi.ExportChartsAsync(host, bearerToken, qParam, ct));
            }

            if (savedQueryIds.Any())
            {
                var qParam = $"[{string.Join(",", savedQueryIds)}]";
                exportTasks.Add(_supersetApi.ExportSavedQueriesAsync(host, bearerToken, qParam, ct));
            }

            if (!exportTasks.Any())
            {
                throw new InvalidOperationException($"Nenhum recurso encontrado para exportar no tenant {job.Payload.TenantId}.");
            }

            var responses = await Task.WhenAll(exportTasks);

            var masterZipStream = new MemoryStream();
            using (var masterZip = new ZipArchive(masterZipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var response in responses)
                {
                    response.EnsureSuccessStatusCode();

                    using var partZipStream = await response.Content.ReadAsStreamAsync(ct);
                    using var partZip = new ZipArchive(partZipStream, ZipArchiveMode.Read);

                    foreach (var entry in partZip.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            continue;
                        }

                        var existingEntry = masterZip.GetEntry(entry.FullName);
                        if (existingEntry != null)
                        {
                            continue;
                        }

                        var newEntry = masterZip.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        using var newEntryStream = newEntry.Open();
                        await entryStream.CopyToAsync(newEntryStream, ct);
                    }
                }
            }

            masterZipStream.Position = 0;

            var user = await _mediator.Send(new GetUserTenantQuery(
                _applicationAccountOptions.ServiceUserId,
                _applicationAccountOptions.TenantId), ct);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Service user not found or unauthorized.");
            }

            var category = await _storedFileService.GetCategoryByIdAsync(
                SupersetYamlMigrationFile.CategoryId,
                user,
                ct);

            if (category == null)
            {
                throw new InvalidOperationException("FileCategory not found.");
            }

            var fileId = Guid.NewGuid();
            var storageRequest = new StoredFileUploadRequest(
                masterZipStream,
                category,
                user,
                $"superset_migration_{job.Payload.TenantId}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip",
                fileId);

            tenantMigration.MarkYamlMigrationFileCreated(fileId);

            await _unitOfWork.BeginAsync(ct);
            try
            {
                await _storageService.UploadAsync(storageRequest, ct);
                await _repository.SaveAsync(tenantMigration, ct);
                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }
        finally
        {
            await _processingLock.ReleaseLock(job.Payload.TenantId, ct);
        }
        
    }

    internal class ExportSupersetTenantMigrationFileJobProducerWithRequest : AbstractPublishProducerWithRequest<
        ExportSupersetTenantMigrationFileJobHandler, SupersetTenantMigrationJobPayload>
    {
        public ExportSupersetTenantMigrationFileJobProducerWithRequest(IBackgroundJobClient backgroundJobClient)
            : base(backgroundJobClient)
        {
        }
    }
    
    
}