// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
﻿using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IAssignedGroupRepository : IBaseRepository<AssignedGroup>
{
    Task<IEnumerable<Group>> Assigned(Guid? id);
}
