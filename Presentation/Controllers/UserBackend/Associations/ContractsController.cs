// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Associations;

/// <summary>
/// CRUD for contract templates (master data such as hours, rates and the holiday calendar). Writing is Admin-only,
/// because the templates are settings (owner decision 2026-11-07: roles are work areas, Admin holds every setting).
/// Assigning a template to an employee is employee maintenance and goes through the client aggregate under
/// CanEditClients, not through this controller. The Post and Put overrides only add the Admin restriction: an
/// [Authorize] on an override is AND-combined with the base Admin/Authorised attribute, so the effective rule is Admin.
/// DELETE is Admin-only in the base already; reads stay open.
/// </summary>
public class ContractsController : InputBaseController<ContractResource>
{
    private readonly ILogger<ContractsController> _logger;

    public ContractsController(IMediator Mediator, ILogger<ContractsController> logger)
        : base(Mediator, logger)
    {
        _logger = logger;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<ContractResource>> Post([FromBody] ContractResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<ContractResource>> Put([FromBody] ContractResource resource) =>
        base.Put(resource);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractResource>>> GetContracts()
    {
        _logger.LogInformation("Fetching all contracts.");
        var contracts = await Mediator.Send(new ListQuery<ContractResource>());
        _logger.LogInformation("Retrieved {Count} contracts.", contracts.Count());
        return Ok(contracts);
    }
}