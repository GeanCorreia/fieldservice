using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FieldService.Form.Controllers;

public class FormController : ControllerBase 
{
    private readonly IMediator _mediator;
    
    public FormController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    
    [HttpGet("forms/{tenantId}")]
    
    [HttpGet("form/{formId}")]
    
    [HttpGet("form-submissions/{formId}/submissions")]
    
    [HttpGet("form-submissions/{formId}/submission/{submissionId}")]
    
}