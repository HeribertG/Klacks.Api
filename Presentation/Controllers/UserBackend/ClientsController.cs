using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class ClientsController : InputBaseController<ClientResource>
{
    public ClientsController(IMediator Mediator, ILogger<ClientsController> logger)
    : base(Mediator, logger)
    {
    }

    [HttpGet("Count")]
    public async Task<IActionResult> CountAsync()
    {
        var count = await Mediator.Send(new CountQuery());
        return Ok(count);
    }

    [HttpGet("FindClient/{company}/{Name}/{firstName}")]
    public async Task<ActionResult<IEnumerable<Client>>> FindClient(string? company = null, string? name = null, string? firstName = null)
    {
        var clients = await Mediator.Send(new FindListQuery(company, name, firstName));
        return Ok(clients);
    }

    [HttpPost("GetSimpleList")]
    public async Task<TruncatedClientResource> GetSimpleList([FromBody] FilterResource filter)
    {
        var truncatedClients = await Mediator.Send(new Application.Queries.Clients.GetTruncatedListQuery(filter));
        return truncatedClients;
    }

    [HttpGet("GetStateTokenList")]
    public async Task<IEnumerable<StateCountryToken>> GetStateTokenList(bool isSelected)
    {
        var tokens = await Mediator.Send(new Application.Queries.Settings.CalendarRules.RuleTokenList(isSelected));
        return tokens;
    }

    [HttpGet("LastChangeMetaData")]
    public async Task<ActionResult<LastChangeMetaDataResource>> LastChangeMetaData()
    {
        var metaData = await Mediator.Send(new LastChangeMetaDataQuery());
        return Ok(metaData);
    }
}
