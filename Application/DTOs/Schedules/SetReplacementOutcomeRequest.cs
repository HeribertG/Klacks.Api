// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Body of PUT Recovery/Requests/{id}/Outcome: the candidate's answer. Proposed is not an answer and is refused.
/// Accepted makes the candidate the slot's replacement in the row's scenario.
/// </summary>
/// <param name="Outcome">Requested, Accepted, Declined or NotReached</param>
/// <param name="OverrideBlock">K1 supervisor override for a Block-mode compliance exclusion of the candidate</param>

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record SetReplacementOutcomeRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter<ReplacementRequestOutcome>))] ReplacementRequestOutcome Outcome,
    bool OverrideBlock = false);
