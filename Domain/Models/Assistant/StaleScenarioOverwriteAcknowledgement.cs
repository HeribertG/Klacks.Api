// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// What the user saw and accepted when they confirmed overwriting the newer changes of a stale scenario's original
/// plan. The accept compares it with the current staleness and asks again when more changed in the meantime.
/// </summary>
/// <param name="ChangedEntryCount">Changed real-plan entries in the accept scope that the user was told about</param>
/// <param name="LastChangeUtc">UTC instant of the most recent of those changes</param>
public sealed record StaleScenarioOverwriteAcknowledgement(int ChangedEntryCount, DateTime LastChangeUtc);
