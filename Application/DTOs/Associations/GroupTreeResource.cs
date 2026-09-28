// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿namespace Klacks.Api.Application.DTOs.Associations;

public class GroupTreeResource
{
    public Guid? RootId { get; set; }

    public List<GroupResource> Nodes { get; set; } = new List<GroupResource>();
}
