// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class AssignedGroupRepository : BaseRepository<AssignedGroup>, IAssignedGroupRepository
{
    private readonly DataBaseContext context;
    private readonly IUserService userService;

    public AssignedGroupRepository(DataBaseContext context, IUserService userService, ILogger<AssignedGroup> logger)
        : base(context, logger)
    {
        this.context = context;
        this.userService = userService;
    }

    public async Task<IEnumerable<Group>> Assigned(Guid? id)
    {
        var currentId = id;
        if (currentId == null)
        {
            currentId = userService.GetId();
        }

        var List = await context.AssignedGroup.Where(x => x.ClientId == currentId).Select(x => x.GroupId).ToListAsync();
        return await context.Group.Where(x => (x.Id == x.Root || !(x.Root.HasValue && x.Parent.HasValue)) && List.Contains(x.Id)).ToListAsync();
    }
}
