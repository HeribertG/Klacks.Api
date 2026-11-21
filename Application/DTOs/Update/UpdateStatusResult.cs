// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.DTOs.Update;

public class UpdateStatusResult
{
    public string CurrentVersion { get; set; } = string.Empty;

    public UpdateAvailability? Availability { get; set; }

    public UpdateHistoryItem? ActiveOperation { get; set; }

    public UpdateHistoryItem? LastOperation { get; set; }

    public int CurrentMajor { get; set; }

    public int? AvailableMajor { get; set; }

    public bool CanStartEngineUpgrade { get; set; }

    public string? EngineUpgradeBlockedReason { get; set; }
}
