using Klacks_api.Models.Staffs;
using Klacks_api.Queries.Clients;
using Klacks_api.Resources.Filter;
using Klacks_api.Resources.Staffs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks_api.Controllers.V1.Backend;

public class ClientsController : InputBaseController<ClientResource>
{
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IMediator mediator, ILogger<ClientsController> logger)
    : base(mediator, logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("Count")]
    public async Task<IActionResult> CountAsync()
    {
        try
        {
            _logger.LogInformation("Fetching client count.");
            var count = await mediator.Send(new CountQuery());
            _logger.LogInformation($"Retrieved client count: {count}");
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching client count.");
            return Problem("An error occurred while fetching the client count.", statusCode: 500);
        }
    }

    [HttpGet("FindClient/{company}/{Name}/{firstName}")]
    public async Task<ActionResult<IEnumerable<Client>>> FindClient(string? company = null, string? name = null, string? firstName = null)
    {
        try
        {
            _logger.LogInformation($"Searching for clients with company: {company}, name: {name}, firstName: {firstName}");
            var clients = await mediator.Send(new FindListQuery(company, name, firstName));
            _logger.LogInformation($"Found {clients.Count()} clients matching criteria.");
            return Ok(clients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching for clients.");
            throw;
        }
    }

    [AllowAnonymous]
    [HttpPost("GetSimpleList")]
    public async Task<TruncatedClient> GetSimpleList1([FromBody] FilterResource filter)
    {
        try
        {
            _logger.LogInformation($"Fetching simple client list with filter: {filter}");
            var truncatedClients = await mediator.Send(new Queries.Clients.GetTruncatedListQuery(filter));
            _logger.LogInformation($"Retrieved {truncatedClients.Clients?.Count} truncated clients.");
            return truncatedClients;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching simple client list.");
            throw;
        }
    }

    [HttpGet("GetStateTokenList")]
    public async Task<IEnumerable<StateCountryToken>> GetStateTokenList(bool isSelected)
    {
        try
        {
            _logger.LogInformation($"Fetching state token list with isSelected: {isSelected}");
            var tokens = await mediator.Send(new Queries.Settings.CalendarRules.RuleTokenList(isSelected));
            _logger.LogInformation($"Retrieved {tokens.Count()} state tokens.");
            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching state token list.");
            throw;
        }
    }

    [HttpGet("LastChangeMetaData")]
    public async Task<ActionResult<LastChangeMetaDataResource>> LastChangeMetaData()
    {
        try
        {
            _logger.LogInformation("Fetching last change metadata.");
            var metaData = await mediator.Send(new LastChangeMetaDataQuery());
            _logger.LogInformation("Retrieved last change metadata.");
            return Ok(metaData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching last change metadata.");
            throw;
        }
    }
}
