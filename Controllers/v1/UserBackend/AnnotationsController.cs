using Klacks.Api.Queries;
using Klacks.Api.Queries.Annotation;
using Klacks.Api.Resources.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.UserBackend;

public class AnnotationsController : InputBaseController<AnnotationResource>
{
    private readonly ILogger<AnnotationsController> logger;

    public AnnotationsController(IMediator mediator, ILogger<AnnotationsController> logger)
      : base(mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnnotationResource>>> GetAnnotation()
    {
        try
        {
            logger.LogInformation("Fetching all annotations.");
            var annotations = await mediator.Send(new ListQuery<AnnotationResource>());
            logger.LogInformation($"Retrieved {annotations.Count()} annotations.");
            return Ok(annotations);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching annotations.");
            throw;
        }
    }

    [HttpGet("GetSimpleAnnotation/{id}")]
    public async Task<ActionResult<IEnumerable<AnnotationResource>>> GetSimpleAnnotation(Guid id)
    {
        try
        {
            logger.LogInformation($"Fetching simple annotations for ID: {id}");
            var annotations = await mediator.Send(new GetSimpleListQuery(id));
            logger.LogInformation($"Retrieved {annotations.Count()} simple annotations for ID: {id}");
            return Ok(annotations);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while fetching simple annotations for ID: {id}");
            throw;
        }
    }
}
