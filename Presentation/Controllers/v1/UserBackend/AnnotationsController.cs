using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class AnnotationsController : InputBaseController<AnnotationResource>
{
    private readonly ILogger<AnnotationsController> _logger;

    public AnnotationsController(IMediator Mediator, ILogger<AnnotationsController> logger)
      : base(Mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnnotationResource>>> GetAnnotation()
    {
        _logger.LogInformation("Fetching all annotations.");
        var annotations = await Mediator.Send(new ListQuery<AnnotationResource>());
        _logger.LogInformation($"Retrieved {annotations.Count()} annotations.");
        return Ok(annotations);
    }

    [HttpGet("GetSimpleAnnotation/{id}")]
    public async Task<ActionResult<IEnumerable<AnnotationResource>>> GetSimpleAnnotation(Guid id)
    {
        _logger.LogInformation($"Fetching simple annotations for ID: {id}");
        var annotations = await Mediator.Send(new GetSimpleListQuery(id));
        _logger.LogInformation($"Retrieved {annotations.Count()} simple annotations for ID: {id}");
        return Ok(annotations);
    }
}
