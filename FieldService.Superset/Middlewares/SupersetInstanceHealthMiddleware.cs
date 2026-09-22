using FieldService.Shared.Services;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Middlewares;

internal class SupersetInstanceHealthMiddleware
{
    private readonly RequestDelegate _next;
    

    public SupersetInstanceHealthMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    internal async Task InvokeAsync(
        HttpContext context, 
        ISupersetService supersetService,
        StartSupersetTenantInstanceCreatedJobProducer startSupersetTenantInstanceCreatedJobProducer,
        CancellationToken cancellationToken)
    {

        //
        // var tenantId = ClaimsResolver.GetTenantId(context.User);
        //
        // if (!await supersetService.HasSuperset(tenantId))
        // {
        //     throw new UnauthorizedAccessException($"Tenant {tenantId} does not have access to Superset.");
        // }
        //
        // var supersetInstance = await supersetService.GetTenantInstance(
        //     tenantId, 
        //     SupersetInstanceStatus.Running, 
        //     cancellationToken);
        //
        // if (supersetInstance == null)
        // {
        //     createSupersetTenantInstanceCreatedJobProducer.Publish(
        //         new CreateSupersetTenantInstanceJob(tenantId));
        //     
        //     throw new SupersetTenantInstanceNotRunningException(tenantId);
        // }
        // // 2. Valida se a instância do Superset daquele Tenant específico está ativa
        // var isRunning = await supersetService.IsInstanceRunningAsync(tenantId, context.RequestAborted);
        //
        // if (!isRunning)
        // {
        //     _logger.LogWarning("Instância do Superset para o Tenant {TenantId} não está em execução.", tenantId);
        //     throw new SupersetTenantInstanceNotRunningException(tenantId);
        // }

        // 3. Permite que a requisição prossiga na pipeline para o YARP
        await _next(context);
    }
}