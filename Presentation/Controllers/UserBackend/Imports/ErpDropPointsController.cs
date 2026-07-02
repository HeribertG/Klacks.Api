// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.ErpDropPoints;
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Imports;

[Authorize(Roles = Roles.Admin)]
public class ErpDropPointsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<ErpDropPointsController> _logger;

    public ErpDropPointsController(IMediator mediator, ILogger<ErpDropPointsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IEnumerable<ErpDropPointListResource>> GetErpDropPoints()
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] GET ErpDropPoints");
        return await _mediator.Send(new ListQuery());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ErpDropPointResource>> GetErpDropPoint(Guid id)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] GET ErpDropPoints/{Id}", id);
        var result = await _mediator.Send(new GetQuery(id));

        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    [HttpPost]
    public async Task<ActionResult<ErpDropPointResource>> PostErpDropPoint([FromBody] ErpDropPointResource resource)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] POST ErpDropPoints - Name: {Name}", resource.Name.ForLog());
        var result = await _mediator.Send(new PostCommand(resource));
        return result!;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ErpDropPointResource>> PutErpDropPoint(Guid id, [FromBody] ErpDropPointResource resource)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] PUT ErpDropPoints - Id: {Id}, Name: {Name}", id, resource.Name.ForLog());
        resource.Id = id;
        var result = await _mediator.Send(new PutCommand(resource));

        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ErpDropPointResource>> DeleteErpDropPoint(Guid id)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] DELETE ErpDropPoints/{Id}", id);
        var result = await _mediator.Send(new DeleteCommand(id));

        if (result == null)
        {
            return NotFound();
        }

        return result;
    }
}
