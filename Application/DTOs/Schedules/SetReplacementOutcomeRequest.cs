// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Body of PUT Recovery/Requests/{id}/Outcome: the candidate's answer. Proposed is not an answer and is refused.
/// </summary>
/// <param name="Outcome">Requested, Accepted, Declined or NotReached</param>

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record SetReplacementOutcomeRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter<ReplacementRequestOutcome>))] ReplacementRequestOutcome Outcome);
