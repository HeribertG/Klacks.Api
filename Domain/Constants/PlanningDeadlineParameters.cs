// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parameter names shared by the planning-deadline skills and their tests.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PlanningDeadlineParameters
{
    public const string DeliveryChannel = "deliveryChannel";
    public const string TransitDays = "transitDays";
    public const string AnnouncementDays = "announcementDays";
    public const string ReviewDays = "reviewDays";
    public const string LeadDays = "leadDays";
}
