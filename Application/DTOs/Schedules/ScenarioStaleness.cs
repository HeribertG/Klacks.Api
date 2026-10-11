// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The real plan changed inside the scope an accept replaces after the scenario was created, so accepting the scenario
/// would silently overwrite those changes (another scenario accepted in between, edits, seals, or a background
/// </summary>
/// <param name="ChangedEntryCount">Real works, work changes, expenses, breaks and schedule notes in the accept scope that were created, updated or deleted after the scenario was created</param>
/// <param name="LastChangeUtc">UTC instant of the most recent of those changes</param>
/// <param name="AcceptedScenarioNames">Other scenarios of the same group with an overlapping range that were accepted after this one was created (a likely cause); empty when none</param>
public sealed record ScenarioStaleness(
    int ChangedEntryCount,
    DateTime LastChangeUtc,
    IReadOnlyList<string> AcceptedScenarioNames);
