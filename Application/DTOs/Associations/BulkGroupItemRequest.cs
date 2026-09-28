// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Associations;

public class BulkGroupItemRequest
{
    public ICollection<GroupItemResource> Items { get; set; } = new List<GroupItemResource>();
}
