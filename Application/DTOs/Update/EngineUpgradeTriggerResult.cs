// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Update;

/// <summary>
/// Answer of the engine upgrade trigger: the id of the queued operation, or the EngineUpgradeBlockedReason name.
/// </summary>
public class EngineUpgradeTriggerResult
{
    public bool Enqueued { get; set; }

    public Guid? OperationId { get; set; }

    public string? BlockedReason { get; set; }
}
