using FieldService.Form.Dtos;
using MediatR;

namespace FieldService.Form.Cqrs.Queries.GetFormSubmissionById;

internal record GetFormSubmissionByIdQuery(
    Guid TenantId,
    Guid SubmissionId) : IRequest<FormSubmissionDto?>;

