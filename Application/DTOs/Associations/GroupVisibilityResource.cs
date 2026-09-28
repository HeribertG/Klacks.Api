// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿namespace Klacks.Api.Application.DTOs.Associations;

public class GroupVisibilityResource
{
    public Guid Id { get; set; }

    public required string AppUserId { get; set; }

    public Guid GroupId { get; set; }
}
