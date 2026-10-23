// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Update;

public class UpdateConfig
{
    public const int DefaultCheckIntervalHours = 6;
    public const int DefaultBackupRetentionCount = 3;

    public bool AutoEnabled { get; set; }

    public string Channel { get; set; } = "Stable";

    public int CheckIntervalHours { get; set; } = DefaultCheckIntervalHours;

    public string MaintenanceWindowStart { get; set; } = string.Empty;

    public string MaintenanceWindowEnd { get; set; } = string.Empty;

    public bool NotifyOnly { get; set; }

    public int BackupRetentionCount { get; set; } = DefaultBackupRetentionCount;

    public string PinnedVersion { get; set; } = string.Empty;
}
