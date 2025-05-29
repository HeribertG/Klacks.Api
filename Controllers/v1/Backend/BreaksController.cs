using Klacks.Api.Queries.Breaks;
using Klacks.Api.Resources.Filter;
using Klacks.Api.Resources.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Controllers.V1.Backend
{
    public class BreaksController : InputBaseController<BreakResource>
    {
        private readonly ILogger<BreaksController> _logger;

        public BreaksController(IMediator mediator, ILogger<BreaksController> logger)
          : base(mediator, logger)
        {
            _logger = logger;
        }

        [HttpPost("GetClientList")]
        public async Task<ActionResult<IEnumerable<ClientBreakResource>>> GetClientList([FromBody] BreakFilter filter)
        {
            _logger.LogInformation($"BreaksController GetClientList Resource: {JsonConvert.SerializeObject(filter)}");
            try
            {
                var clientList = await mediator.Send(new ListQuery(filter));
                _logger.LogInformation($"Retrieved {clientList.Count()} client break resources.");
                return Ok(clientList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching client break list.");
                throw;
            }
        }
    }
}
