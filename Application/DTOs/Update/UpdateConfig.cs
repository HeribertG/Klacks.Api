// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Update;

public class UpdateConfig
{
    public bool AutoEnabled { get; set; }

    public string Channel { get; set; } = "Stable";

    public int CheckIntervalHours { get; set; } = 6;

    public string MaintenanceWindowStart { get; set; } = string.Empty;

    public string MaintenanceWindowEnd { get; set; } = string.Empty;

    public bool NotifyOnly { get; set; }

    public int BackupRetentionCount { get; set; } = 3;

    public string PinnedVersion { get; set; } = string.Empty;
}
