// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Associations;

public class BulkGroupItemRequest
{
    public ICollection<GroupItemResource> Items { get; set; } = new List<GroupItemResource>();
}
