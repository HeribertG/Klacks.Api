// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Application.Queries.AssignedGroups;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Staffs;

public class AssignedGroupsController(IMediator mediator, ILogger<AssignedGroupsController> logger) : InputBaseController<GroupResource>(mediator, logger)
{
    [HttpGet("list")]
    public async Task<IEnumerable<GroupResource>> Get(Guid? id)
    {
        return await Mediator.Send(new AssignedGroupListQuery(id));
    }
}
