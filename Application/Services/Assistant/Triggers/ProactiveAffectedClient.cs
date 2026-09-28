// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One employee named inside an aggregated proactive finding. Carries the id so the event payload stays
/// actionable and the display name so the summary sentence can list who is affected without a second
/// lookup at render time.
/// </summary>
namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record ProactiveAffectedClient(Guid ClientId, string ClientName);
