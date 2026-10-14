// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of the cancel command: the outcome decides the HTTP status (Forbidden answers 403), Result is the body of
/// the other outcomes.
/// </summary>
/// <param name="Outcome">Cancelled, already resolved or not allowed for this caller</param>
/// <param name="Result">Body for the Cancelled and AlreadyResolved outcomes</param>

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Assistant;

public record EscalationCancelResultResource(EscalationCancelOutcome Outcome, HttpResultResource Result);
