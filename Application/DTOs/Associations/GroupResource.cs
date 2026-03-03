// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.ObjectModel;

namespace Klacks.Api.Application.DTOs.Associations;

public class GroupResource : SimpleGroupResource
{
    public GroupResource()
    {
        GroupItems = new Collection<GroupItemResource>();
        Children = new List<GroupResource>();
    }

    public ICollection<GroupItemResource> GroupItems { get; set; }

    public List<GroupResource> Children { get; set; }

    public Guid? Parent { get; set; }

    public Guid? Root { get; set; }

    public int Lft { get; set; }

    public int Rgt { get; set; }

    public int Depth { get; set; }

    public int ClientsCount
    {
        get
        {
            return GroupItems?.Count ?? 0;
        }
    }

    public int ShiftsCount { get; set; }

    public int CustomersCount { get; set; }
}