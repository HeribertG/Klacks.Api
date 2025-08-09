﻿namespace Klacks.Api.Presentation.DTOs.Staffs;

public class AssignedGroupResource
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public Guid GroupId { get; set; }

    public string GroupName { get; set; } = null!;
}
