// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿using Klacks.Api.Application.Queries.AssignedGroups;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Staffs;

/// <summary>
/// The groups a client is assigned to (read) on the generic group CRUD. Post and Put write a group itself, the same
/// commands as GroupsController, so they are Admin-only like there: the group structure is a setting (owner decision
/// 2026-11-07). The overrides only add the Admin restriction to the inherited Admin/Authorised attribute.
/// </summary>
public class AssignedGroupsController(IMediator mediator, ILogger<AssignedGroupsController> logger) : InputBaseController<GroupResource>(mediator, logger)
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<GroupResource>> Post([FromBody] GroupResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<GroupResource>> Put([FromBody] GroupResource resource) =>
        base.Put(resource);

    [HttpGet("list")]
    public async Task<IEnumerable<GroupResource>> Get(Guid? id)
    {
        return await Mediator.Send(new AssignedGroupListQuery(id));
    }
}
