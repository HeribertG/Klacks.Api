using Klacks.Api.Queries.Breaks;
using Klacks.Api.Presentation.Resources.Filter;
using Klacks.Api.Presentation.Resources.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;
    public class BreaksController : InputBaseController<BreakResource>
    {
        private readonly ILogger<BreaksController> logger;

        public BreaksController(IMediator Mediator, ILogger<BreaksController> logger)
          : base(Mediator, logger)
        {
            this.logger = logger;
        }

        [HttpPost("GetClientList")]
        public async Task<ActionResult<IEnumerable<ClientBreakResource>>> GetClientList([FromBody] BreakFilter filter)
        {
            logger.LogInformation($"BreaksController GetClientList Resource: {JsonConvert.SerializeObject(filter)}");
            try
            {
                var clientList = await Mediator.Send(new ListQuery(filter));
                logger.LogInformation($"Retrieved {clientList.Count()} client break resources.");
                return Ok(clientList);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching client break list.");
                throw;
            }
        }
    }
