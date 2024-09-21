using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using Klacks.Api.Resources.Filter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Controllers.V1.Backend;

public class GroupsController : InputBaseController<GroupResource>
{
  private readonly ILogger<GroupsController> _logger;

  public GroupsController(IMediator mediator, ILogger<GroupsController> logger)
    : base(mediator, logger)
  {
    _logger = logger;
  }

  [HttpPost("GetSimpleList")]
  public async Task<ActionResult<TruncatedGroupResource>> GetSimpleList([FromBody] GroupFilter filter)
  {
    try
    {
      _logger.LogInformation($"Fetching simple group list with filter: {JsonConvert.SerializeObject(filter)}");
      var truncatedGroups = await mediator.Send(new GetTruncatedListQuery(filter));
      _logger.LogInformation($"Retrieved {truncatedGroups.Groups.Count} truncated groups.");
      return Ok(truncatedGroups);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while fetching simple group list.");
      throw;
    }
  }
}
