// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class HeartbeatDefaults
{
    public const int DefaultIntervalMinutes = 30;

    public static readonly TimeOnly DefaultActiveHoursStart = new(8, 0);
    public static readonly TimeOnly DefaultActiveHoursEnd = new(18, 0);

    public const string DefaultChecklistJson =
        "[{\"Label\":\"Unread messages or notifications\",\"Category\":\"system\",\"IsEnabled\":true}," +
        "{\"Label\":\"Pending absence requests\",\"Category\":\"scheduling\",\"IsEnabled\":true}," +
        "{\"Label\":\"Schedule conflicts for today\",\"Category\":\"scheduling\",\"IsEnabled\":true}," +
        "{\"Label\":\"System health status\",\"Category\":\"system\",\"IsEnabled\":true}]";

    public static class Categories
    {
        public const string Scheduling = "scheduling";
        public const string Employees = "employees";
        public const string System = "system";
        public const string Custom = "custom";
    }
}
