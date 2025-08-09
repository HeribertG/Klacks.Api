using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class AnnotationsController : InputBaseController<AnnotationResource>
{
    private readonly ILogger<AnnotationsController> logger;

    public AnnotationsController(IMediator Mediator, ILogger<AnnotationsController> logger)
      : base(Mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnnotationResource>>> GetAnnotation()
    {
        try
        {
            logger.LogInformation("Fetching all annotations.");
            var annotations = await Mediator.Send(new ListQuery<AnnotationResource>());
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
            var annotations = await Mediator.Send(new GetSimpleListQuery(id));
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
