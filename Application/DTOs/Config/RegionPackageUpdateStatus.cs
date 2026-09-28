// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Config;

public class RegionPackageUpdateStatus
{
    public DateTime LastCheckUtc { get; set; }

    public string InstalledVersion { get; set; } = string.Empty;

    public string? AvailableVersion { get; set; }

    public string LastResult { get; set; } = string.Empty;

    public string? LastError { get; set; }
}
