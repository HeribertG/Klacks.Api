using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ClientsController : InputBaseController<ClientResource>
{
    private readonly ILogger<ClientsController> logger;

    public ClientsController(IMediator Mediator, ILogger<ClientsController> logger)
    : base(Mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet("Count")]
    public async Task<IActionResult> CountAsync()
    {
        logger.LogInformation("Fetching client count.");
        var count = await Mediator.Send(new CountQuery());
        logger.LogInformation($"Retrieved client count: {count}");
        return Ok(count);
    }

    [HttpGet("FindClient/{company}/{Name}/{firstName}")]
    public async Task<ActionResult<IEnumerable<Client>>> FindClient(string? company = null, string? name = null, string? firstName = null)
    {
        logger.LogInformation($"Searching for clients with company: {company}, name: {name}, firstName: {firstName}");
        var clients = await Mediator.Send(new FindListQuery(company, name, firstName));
        logger.LogInformation($"Found {clients.Count()} clients matching criteria.");
        return Ok(clients);
    }

    [HttpPost("GetSimpleList")]
    public async Task<TruncatedClientResource> GetSimpleList([FromBody] FilterResource filter)
    {
        logger.LogInformation($"Fetching simple client list with filter: {filter}");
        var truncatedClients = await Mediator.Send(new Application.Queries.Clients.GetTruncatedListQuery(filter));
        logger.LogInformation($"Retrieved {truncatedClients.Clients?.Count} truncated clients.");
        return truncatedClients;
    }

    [HttpGet("GetStateTokenList")]
    public async Task<IEnumerable<StateCountryToken>> GetStateTokenList(bool isSelected)
    {
        logger.LogInformation($"Fetching state token list with isSelected: {isSelected}");
        var tokens = await Mediator.Send(new Application.Queries.Settings.CalendarRules.RuleTokenList(isSelected));
        logger.LogInformation($"Retrieved {tokens.Count()} state tokens.");
        return tokens;
    }

    [HttpGet("LastChangeMetaData")]
    public async Task<ActionResult<LastChangeMetaDataResource>> LastChangeMetaData()
    {
        logger.LogInformation("Fetching last change metadata.");
        var metaData = await Mediator.Send(new LastChangeMetaDataQuery());
        logger.LogInformation("Retrieved last change metadata.");
        return Ok(metaData);
    }
}
