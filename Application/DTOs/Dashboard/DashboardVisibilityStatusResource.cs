// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Dashboard;

public class DashboardVisibilityStatusResource
{
    public bool IsRestricted { get; set; }

    public bool HasVisibleGroups { get; set; }
}
