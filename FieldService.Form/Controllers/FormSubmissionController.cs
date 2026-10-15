using System.Text.Json;
using FieldService.Authorization.Attributes;
using FieldService.Form.Cqrs.Commands.CreateFormSubmission;
using FieldService.Form.Cqrs.Commands.DeleteFormSubmission;
using FieldService.Form.Cqrs.Commands.UpdateFormSubmission;
using FieldService.Form.Cqrs.Queries.GetDeletedFormGridData;
using FieldService.Form.Cqrs.Queries.GetFormGridData;
using FieldService.Form.Cqrs.Queries.GetFormSubmissionById;
using FieldService.Form.Cqrs.Queries.GetUnfilteredFormGridData;
using FieldService.Form.Dtos;
using FieldService.FormIO.Attributes;
using FieldService.Shared.Services;
using FieldService.Shared.Types.Http;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FieldService.Form.Controllers;

[ApiController]
public class FormSubmissionController : ControllerBase
{
    private readonly IMediator _mediator;

    public FormSubmissionController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet("forms/{formId:guid}/submissions")]
    [RequirePermissionAttribute(FormPermissions.SubmissionView)]
    public async Task<IActionResult> GetSubmissions(
        [FromRoute] Guid formId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isDescending = false,
        CancellationToken cancellationToken = default)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var query = new GetUnfilteredFormGridDataQuery(
            tenantId,
            formId,
            new PagedRequest(page, pageSize, sortBy, isDescending));

        var response = await _mediator.Send(query, cancellationToken);
        return Ok(response);
    }

    [HttpPost("forms/{formId:guid}/submissions/filter")]
    [RequirePermissionAttribute(FormPermissions.SubmissionView)]
    public async Task<IActionResult> GetFilteredSubmissions(
        [FromRoute] Guid formId,
        [FromBody] FormGridFilterBody request,
        CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);

        var filter = new FormGridFilterRequest(
            TenantId: tenantId,
            FormId: formId,
            Filters: request.Filters,
            PagedRequest: request.PagedRequest);

        var response = await _mediator.Send(new GetFormGridDataQuery(tenantId, filter), cancellationToken);
        return Ok(response);
    }

    [HttpGet("forms/{formId:guid}/submissions/deleted")]
    [RequirePermissionAttribute(FormPermissions.SubmissionView)]
    public async Task<IActionResult> GetDeletedSubmissions([FromRoute] Guid formId, CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var response = await _mediator.Send(new GetDeletedFormGridDataQuery(tenantId, formId), cancellationToken);
        return Ok(response);
    }

    [HttpGet("forms/{formId:guid}/submissions/{submissionId:guid}")]
    [RequirePermissionAttribute(FormPermissions.SubmissionView)]
    public async Task<IActionResult> GetSubmissionById(
        [FromRoute] Guid formId,
        [FromRoute] Guid submissionId,
        CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var response = await _mediator.Send(new GetFormSubmissionByIdQuery(tenantId, submissionId), cancellationToken);

        if (response is null || response.FormId != formId)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("forms/{formId:guid}/submissions")]
    [RequirePermissionAttribute(FormPermissions.SubmissionCreate)]
    public async Task<IActionResult> CreateSubmission(
        [FromRoute] Guid formId,
        [FromBody] SaveSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(User);
        var tenantId = ClaimsResolver.GetTenantId(User);

        await _mediator.Send(
            new CreateFormSubmissionCommand(tenantId, formId, userId, request.DataJson),
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("forms/{formId:guid}/submissions/{submissionId:guid}")]
    [RequirePermissionAttribute(FormPermissions.SubmissionEdit)]
    public async Task<IActionResult> UpdateSubmission(
        [FromRoute] Guid formId,
        [FromRoute] Guid submissionId,
        [FromBody] SaveSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(User);
        var tenantId = ClaimsResolver.GetTenantId(User);

        await _mediator.Send(
            new UpdateFormSubmissionCommand(tenantId, submissionId, userId, request.DataJson),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("forms/{formId:guid}/submissions/{submissionId:guid}")]
    [RequirePermissionAttribute(FormPermissions.SubmissionDelete)]
    public async Task<IActionResult> DeleteSubmission(
        [FromRoute] Guid formId,
        [FromRoute] Guid submissionId,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(User);
        var tenantId = ClaimsResolver.GetTenantId(User);

       await _mediator.Send(new DeleteFormSubmissionCommand(tenantId, submissionId, userId), cancellationToken);
        return NoContent();
    }
}

public record SaveSubmissionRequest(JsonElement DataJson);

public record FormGridFilterBody(
    IEnumerable<FormFieldData> Filters,
    PagedRequest? PagedRequest = null);


