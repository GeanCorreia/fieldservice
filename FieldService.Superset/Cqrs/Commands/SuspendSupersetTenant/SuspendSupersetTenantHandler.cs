using FieldService.Data.Interfaces;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using MediatR;

namespace FieldService.Superset.Cqrs.Commands.SuspendSupersetTenant;

internal class SuspendSupersetTenantHandler : IRequestHandler<SuspendSupersetTenantCommand>
{
    private readonly SuspendSupersetTenantJobProducer _suspendSupersetTenantJobProducer;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly IUnitOfWork _unitOfWork;
    public SuspendSupersetTenantHandler(
        SuspendSupersetTenantJobProducer suspendSupersetTenantJobProducer,
        ISupersetTenantService supersetTenantService,
        IUnitOfWork unitOfWork)
    {
        _suspendSupersetTenantJobProducer = suspendSupersetTenantJobProducer ?? throw new ArgumentNullException(nameof(suspendSupersetTenantJobProducer));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task Handle(
        SuspendSupersetTenantCommand request, 
        CancellationToken cancellationToken)
    {
        var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(request.TenantId, cancellationToken);
        if (supersetTenant == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        if(supersetTenant.Status == SupersetTenantStatus.Suspended)
        {
            throw new InvalidOperationException($"Superset tenant with ID '{request.TenantId}' is already suspended.");
        }
        
        var jobPayload = new SuspendSupersetTenantJobPayload(request.TenantId);
        var job = new SuspendSupersetTenantJob(jobPayload);
        _suspendSupersetTenantJobProducer.Publish(job);
       
    }
}