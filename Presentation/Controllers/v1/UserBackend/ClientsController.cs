using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ClientsController : InputBaseController<ClientResource>
{
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IMediator Mediator, ILogger<ClientsController> logger)
    : base(Mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet("Count")]
    public async Task<IActionResult> CountAsync()
    {
        _logger.LogInformation("Fetching client count.");
        var count = await Mediator.Send(new CountQuery());
        _logger.LogInformation($"Retrieved client count: {count}");
        return Ok(count);
    }

    [HttpGet("FindClient/{company}/{Name}/{firstName}")]
    public async Task<ActionResult<IEnumerable<Client>>> FindClient(string? company = null, string? name = null, string? firstName = null)
    {
        _logger.LogInformation($"Searching for clients with company: {company}, name: {name}, firstName: {firstName}");
        var clients = await Mediator.Send(new FindListQuery(company, name, firstName));
        _logger.LogInformation($"Found {clients.Count()} clients matching criteria.");
        return Ok(clients);
    }

    [HttpPost("GetSimpleList")]
    public async Task<TruncatedClientResource> GetSimpleList([FromBody] FilterResource filter)
    {
        _logger.LogInformation($"Fetching simple client list with filter: {filter}");
        var truncatedClients = await Mediator.Send(new Application.Queries.Clients.GetTruncatedListQuery(filter));
        _logger.LogInformation($"Retrieved {truncatedClients.Clients?.Count} truncated clients.");
        return truncatedClients;
    }

    [HttpGet("GetStateTokenList")]
    public async Task<IEnumerable<StateCountryToken>> GetStateTokenList(bool isSelected)
    {
        _logger.LogInformation($"Fetching state token list with isSelected: {isSelected}");
        var tokens = await Mediator.Send(new Application.Queries.Settings.CalendarRules.RuleTokenList(isSelected));
        _logger.LogInformation($"Retrieved {tokens.Count()} state tokens.");
        return tokens;
    }

    [HttpGet("LastChangeMetaData")]
    public async Task<ActionResult<LastChangeMetaDataResource>> LastChangeMetaData()
    {
        _logger.LogInformation("Fetching last change metadata.");
        var metaData = await Mediator.Send(new LastChangeMetaDataQuery());
        _logger.LogInformation("Retrieved last change metadata.");
        return Ok(metaData);
    }
}
