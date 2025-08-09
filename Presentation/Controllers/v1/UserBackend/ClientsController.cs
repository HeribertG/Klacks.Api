using Klacks.Api.Models.Staffs;
using Klacks.Api.Queries.Clients;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    [AllowAnonymous]
    [HttpGet("Count")]
    public async Task<IActionResult> CountAsync()
    {
        try
        {
            logger.LogInformation("Fetching client count.");
            var count = await Mediator.Send(new CountQuery());
            logger.LogInformation($"Retrieved client count: {count}");
            return Ok(count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching client count.");
            return Problem("An error occurred while fetching the client count.", statusCode: 500);
        }
    }

    [HttpGet("FindClient/{company}/{Name}/{firstName}")]
    public async Task<ActionResult<IEnumerable<Client>>> FindClient(string? company = null, string? name = null, string? firstName = null)
    {
        try
        {
            logger.LogInformation($"Searching for clients with company: {company}, name: {name}, firstName: {firstName}");
            var clients = await Mediator.Send(new FindListQuery(company, name, firstName));
            logger.LogInformation($"Found {clients.Count()} clients matching criteria.");
            return Ok(clients);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while searching for clients.");
            throw;
        }
    }

    [AllowAnonymous]
    [HttpPost("GetSimpleList")]
    public async Task<TruncatedClientResource> GetSimpleList1([FromBody] FilterResource filter)
    {
        try
        {
            logger.LogInformation($"Fetching simple client list with filter: {filter}");
            var truncatedClients = await Mediator.Send(new Queries.Clients.GetTruncatedListQuery(filter));
            logger.LogInformation($"Retrieved {truncatedClients.Clients?.Count} truncated clients.");
            return truncatedClients;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching simple client list.");
            throw;
        }
    }

    [HttpGet("GetStateTokenList")]
    public async Task<IEnumerable<StateCountryToken>> GetStateTokenList(bool isSelected)
    {
        try
        {
            logger.LogInformation($"Fetching state token list with isSelected: {isSelected}");
            var tokens = await Mediator.Send(new Queries.Settings.CalendarRules.RuleTokenList(isSelected));
            logger.LogInformation($"Retrieved {tokens.Count()} state tokens.");
            return tokens;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching state token list.");
            throw;
        }
    }

    [HttpGet("LastChangeMetaData")]
    public async Task<ActionResult<LastChangeMetaDataResource>> LastChangeMetaData()
    {
        try
        {
            logger.LogInformation("Fetching last change metadata.");
            var metaData = await Mediator.Send(new LastChangeMetaDataQuery());
            logger.LogInformation("Retrieved last change metadata.");
            return Ok(metaData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching last change metadata.");
            throw;
        }
    }
}
