// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿namespace Klacks.Api.Application.DTOs.Associations;

public class GroupTreeResource
{
    public Guid? RootId { get; set; }

    public List<GroupResource> Nodes { get; set; } = new List<GroupResource>();
}
