// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// CRUD for clients. Deliberately not an InputBaseController: the note card of edit-address saves its
/// annotations through the client aggregate (PUT api/backend/Clients), so Put has to be reachable by a
/// caller without any role (Planer), and an attribute on an override is AND-combined with the base
/// method's rather than replacing it — the restriction could not be lifted by overriding. Post and
/// Delete keep the Admin/Authorised restriction; Put is open to every authenticated caller and gated
/// in the body: CanEditClients passes straight through, CanEditClientNotes only when the incoming
/// resource differs from the stored client in nothing but its annotations.
/// </summary>
/// <param name="mediator">Dispatches the client queries and commands</param>
/// <param name="changeScopeEvaluator">Decides whether an update reaches beyond the annotations</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces.Staffs;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Staffs;

[ApiController]
public class ClientsController : BaseController, ICrudResourceController<ClientResource>
{
    private readonly IMediator _mediator;
    private readonly IClientChangeScopeEvaluator _changeScopeEvaluator;

    public ClientsController(IMediator mediator, IClientChangeScopeEvaluator changeScopeEvaluator)
    {
        _mediator = mediator;
        _changeScopeEvaluator = changeScopeEvaluator;
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ClientResource>> Get([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetQuery<ClientResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<ClientResource>> Post([FromBody] ClientResource resource)
    {
        var model = await _mediator.Send(new PostCommand<ClientResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ClientResource>> Put([FromBody] ClientResource resource, CancellationToken cancellationToken)
    {
        var rights = User.GetUserRights();

        if (!Permissions.HasPermission(rights, Permissions.CanEditClients))
        {
            if (!Permissions.HasPermission(rights, Permissions.CanEditClientNotes))
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            var scope = await _changeScopeEvaluator.EvaluateAsync(resource, cancellationToken);
            if (scope == ClientChangeScope.NotFound)
            {
                return NotFound();
            }

            if (scope != ClientChangeScope.AnnotationsOnly)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
        }

        var model = await _mediator.Send(new PutCommand<ClientResource>(resource));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<ClientResource>> Delete(Guid id)
    {
        var model = await _mediator.Send(new DeleteCommand<ClientResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpGet("Count")]
    public async Task<IActionResult> CountAsync()
    {
        var count = await _mediator.Send(new CountQuery());
        return Ok(count);
    }

    [HttpGet("ForReplacement")]
    public async Task<ActionResult<IEnumerable<ClientForReplacementResource>>> GetClientsForReplacement()
    {
        var clients = await _mediator.Send(new GetClientsForReplacementQuery());
        return Ok(clients);
    }

    [HttpGet("FindClient/{company}/{Name}/{firstName}")]
    public async Task<ActionResult<IEnumerable<ClientResource>>> FindClient(string? company = null, string? name = null, string? firstName = null)
    {
        var clients = await _mediator.Send(new FindListQuery(company, name, firstName));
        return Ok(clients);
    }

    [HttpPost("GetSimpleList")]
    public async Task<TruncatedClientResource> GetSimpleList([FromBody] FilterResource filter)
    {
        var truncatedClients = await _mediator.Send(new Application.Queries.Clients.GetTruncatedListQuery(filter));
        return truncatedClients;
    }

    [HttpGet("GetStateTokenList")]
    public async Task<IEnumerable<StateCountryToken>> GetStateTokenList(bool isSelected)
    {
        var tokens = await _mediator.Send(new Application.Queries.Settings.CalendarRules.RuleTokenList(isSelected));
        return tokens;
    }

    [HttpGet("LastChangeMetaData")]
    public async Task<ActionResult<LastChangeMetaDataResource>> LastChangeMetaData()
    {
        var metaData = await _mediator.Send(new LastChangeMetaDataQuery());
        return Ok(metaData);
    }

    [HttpPost("ExportList")]
    public async Task<ActionResult<List<ExportClientItemDto>>> ExportList([FromBody] ExportClientRequest request, CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ExportClientListQuery(request), cancellationToken);
        return Ok(items);
    }
}
