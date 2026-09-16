using FieldService.DocumentSupportManagement.Cqrs.Commands.Download;
using FieldService.DocumentSupportManagement.Cqrs.Commands.PresignedDownload;
using FieldService.DocumentSupportManagement.Cqrs.Commands.PresignedUpload;
using FieldService.DocumentSupportManagement.Cqrs.Commands.Upload;
using FieldService.Shared.Services;
using FieldService.Storage.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FieldService.DocumentSupportManagement.Controllers;

[ApiController]
public class DsmController : ControllerBase
{
    private readonly IMediator _mediator;

    public DsmController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    
    [Consumes("multipart/form-data")]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(User);
        var tenantId = ClaimsResolver.GetTenantId(User);
        
        var command = new UploadCommand(
            userId,
            tenantId,
            file.OpenReadStream(),
            file.FileName
        );
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(User);
        var tenantId = ClaimsResolver.GetTenantId(User);
        
        var command = new DownloadCommand(id, userId, tenantId);
        
        var result = await _mediator.Send(command, cancellationToken);
        
        return File(
            fileStream: result.Content,
            contentType: result.File.ContentType.ToString() ?? "application/octet-stream",
            fileDownloadName: result.File.FileName,
            enableRangeProcessing: true 
        );
    }
    
    [HttpPost("presigned-upload")]
    public async Task<IActionResult> PresignedUpload([FromBody] PresignedFileUploadRequestDto request, CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(User);
        var tenantId = ClaimsResolver.GetTenantId(User);
        var command = new PresignedUploadCommand(
            userId,
            tenantId,
            request
        );
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    
    [HttpGet("presigned-download/{id}")]
    public async Task<IActionResult> PresignedUploadGet([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(User);
        var tenantId = ClaimsResolver.GetTenantId(User);
        var command = new PresignedDownloadCommand(
            userId,
            tenantId,
            new PresignedFileDownloadRequestDto(id)
        );
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}