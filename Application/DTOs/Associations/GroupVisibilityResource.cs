// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿namespace Klacks.Api.Application.DTOs.Associations;

public class GroupVisibilityResource
{
    public Guid Id { get; set; }

    public required string AppUserId { get; set; }

    public Guid GroupId { get; set; }
}
