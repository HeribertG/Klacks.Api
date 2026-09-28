// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿namespace Klacks.Api.Application.DTOs.Staffs;

public class AssignedGroupResource
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public Guid GroupId { get; set; }

    public string GroupName { get; set; } = null!;
}
