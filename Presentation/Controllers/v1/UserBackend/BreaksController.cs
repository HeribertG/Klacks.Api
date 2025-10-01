using Klacks.Api.Application.Queries.Breaks;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;
    public class BreaksController : InputBaseController<BreakResource>
    {
        private readonly ILogger<BreaksController> _logger;

        public BreaksController(IMediator Mediator, ILogger<BreaksController> logger)
          : base(Mediator, logger)
        {
            this._logger = logger;
        }

        [HttpPost("GetClientList")]
        public async Task<ActionResult<IEnumerable<ClientBreakResource>>> GetClientList([FromBody] BreakFilter filter)
        {
            _logger.LogInformation($"BreaksController GetClientList Resource: {JsonConvert.SerializeObject(filter)}");
            var clientList = await Mediator.Send(new ListQuery(filter));
            _logger.LogInformation($"Retrieved {clientList.Count()} client break resources.");
            return Ok(clientList);
        }
    }
