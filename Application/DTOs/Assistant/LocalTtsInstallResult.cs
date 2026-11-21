// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of a local voice installation request: the outcome decides the HTTP status (202, 400 or 409), the operation
/// id names the queued update_history row, and the reason is an English message for the settings card.
/// </summary>
namespace Klacks.Api.Application.DTOs.Assistant;

public record LocalTtsInstallResult(LocalTtsInstallOutcome Outcome, Guid? OperationId, string Reason);
