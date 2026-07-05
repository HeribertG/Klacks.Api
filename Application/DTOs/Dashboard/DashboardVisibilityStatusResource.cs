// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Dashboard;

public class DashboardVisibilityStatusResource
{
    public bool IsRestricted { get; set; }

    public bool HasVisibleGroups { get; set; }
}
