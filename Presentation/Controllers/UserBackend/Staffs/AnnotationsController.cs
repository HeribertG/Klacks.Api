using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Staffs;

public class AnnotationsController : InputBaseController<AnnotationResource>
{
    public AnnotationsController(IMediator Mediator, ILogger<AnnotationsController> logger)
      : base(Mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnnotationResource>>> GetAnnotation()
    {
        var annotations = await Mediator.Send(new ListQuery<AnnotationResource>());
        return Ok(annotations);
    }

    [HttpGet("GetSimpleAnnotation/{id}")]
    public async Task<ActionResult<IEnumerable<AnnotationResource>>> GetSimpleAnnotation(Guid id)
    {
        var annotations = await Mediator.Send(new GetSimpleListQuery(id));
        return Ok(annotations);
    }
}
