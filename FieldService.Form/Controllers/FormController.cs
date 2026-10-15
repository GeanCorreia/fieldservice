using System.Text.Json;
using FieldService.Authorization.Attributes;
using FieldService.Form.Cqrs.Commands.CreateForm;
using FieldService.Form.Cqrs.Commands.DeleteForm;
using FieldService.Form.Cqrs.Commands.UpdateForm;
using FieldService.Form.Cqrs.Queries.GetForm;
using FieldService.Form.Cqrs.Queries.GetFormInfos;
using FieldService.Form.Cqrs.Queries.GetForms;
using FieldService.Form.Dtos;
using FieldService.FormIO.Attributes;
using FieldService.Shared.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FieldService.Form.Controllers;

[ApiController]
public class FormController : ControllerBase
{
	private readonly IMediator _mediator;

	public FormController(IMediator mediator)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
	}

	[HttpGet("forms")]
	[RequirePermissionAttribute(FormPermissions.View)]
	public async Task<IActionResult> GetForms(CancellationToken cancellationToken)
	{
		var tenantId = ClaimsResolver.GetTenantId(User);
		var forms = await _mediator.Send(new GetFormsQuery(tenantId), cancellationToken);
		return Ok(forms);
	}

	[HttpGet("forms/infos")]
	[RequirePermissionAttribute(FormPermissions.View)]
	public async Task<IActionResult> GetFormInfos(CancellationToken cancellationToken)
	{
		var tenantId = ClaimsResolver.GetTenantId(User);
		var forms = await _mediator.Send(new GetFormInfosQuery(tenantId), cancellationToken);
		return Ok(forms);
	}

	[HttpGet("forms/{formId:guid}")]
	[RequirePermissionAttribute(FormPermissions.View)]
	public async Task<IActionResult> GetFormById([FromRoute] Guid formId, CancellationToken cancellationToken)
	{
		var tenantId = ClaimsResolver.GetTenantId(User);
		var form = await _mediator.Send(new GetFormQuery(tenantId, formId), cancellationToken);
		return Ok(form);
	}

	[HttpPost("forms")]
	[RequirePermissionAttribute(FormPermissions.Create)]
	public async Task<IActionResult> CreateForm([FromBody] SaveFormRequest request, CancellationToken cancellationToken)
	{
		var userId = ClaimsResolver.GetUserId(User);
		var tenantId = ClaimsResolver.GetTenantId(User);

		var form = new FormDto(
			id: Guid.NewGuid(),
			tenantId: tenantId,
			slug: request.Slug,
			title: request.Title,
			schemaJson: request.SchemaJson,
			isActive: true,
			description: request.Description);

		await _mediator.Send(new CreateFormCommand(tenantId, form, userId), cancellationToken);
		return NoContent();
	}

	[HttpPatch("forms/{formId:guid}")]
	[RequirePermissionAttribute(FormPermissions.Edit)]
	public async Task<IActionResult> UpdateForm(
		[FromRoute] Guid formId,
		[FromBody] SaveFormRequest request,
		CancellationToken cancellationToken)
	{
		var userId = ClaimsResolver.GetUserId(User);
		var tenantId = ClaimsResolver.GetTenantId(User);

		var form = new FormDto(
			id: formId,
			tenantId: tenantId,
			slug: request.Slug,
			title: request.Title,
			schemaJson: request.SchemaJson,
			isActive: true,
			description: request.Description);

		await _mediator.Send(new UpdateFormCommand(tenantId, form, userId), cancellationToken);
		return NoContent();
	}

	[HttpDelete("forms/{formId:guid}")]
	[RequirePermissionAttribute(FormPermissions.Delete)]
	public async Task<IActionResult> DeleteForm([FromRoute] Guid formId, CancellationToken cancellationToken)
	{
		var userId = ClaimsResolver.GetUserId(User);
		var tenantId = ClaimsResolver.GetTenantId(User);

		await _mediator.Send(new DeleteFormCommand(tenantId, formId, userId), cancellationToken);
		return NoContent();
	}
}

public record SaveFormRequest(
	string Slug,
	string Title,
	JsonElement SchemaJson,
	string? Description);
