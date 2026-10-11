// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A consumed held invocation: the skill and parameters confirm_pending_action replays, plus the Purpose of the row
/// it came from (PendingConfirmationPurposes), so the redeemer can tell a gate replay from a correction undo.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record PendingConfirmation(
    Guid UserId,
    string SkillName,
    IReadOnlyDictionary<string, object> Parameters,
    DateTime ExpiresAtUtc,
    string Purpose = PendingConfirmationPurposes.GateReplay);
