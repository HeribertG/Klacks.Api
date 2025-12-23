using Klacks.Api.Application.Commands;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class WorksController : InputBaseController<WorkResource>
{
    public WorksController(IMediator mediator, ILogger<WorksController> logger)
            : base(mediator, logger)
    {
    }

    [HttpPost("Bulk")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<BulkWorksResponse>> BulkAdd([FromBody] BulkAddWorksRequest request)
    {
        var response = await Mediator.Send(new BulkAddWorksCommand(request));
        return Ok(response);
    }

    [HttpDelete("Bulk")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<BulkWorksResponse>> BulkDelete([FromBody] BulkDeleteWorksRequest request)
    {
        var response = await Mediator.Send(new BulkDeleteWorksCommand(request));
        return Ok(response);
    }
}
