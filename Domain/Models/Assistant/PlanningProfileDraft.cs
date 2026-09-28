// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A custom planning profile the assistant is collecting from the admin across dialog turns: the
/// string-valued parameters gathered so far (base-industry choice plus the optional field overrides).
/// Held in the pending draft store keyed by user and conversation; each successful Set slides
/// <see cref="ExpiresAtUtc"/> forward by the store TTL. Nothing is persisted to the domain until apply.
/// </summary>

using System;
using System.Collections.Generic;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class PlanningProfileDraft
{
    public Dictionary<string, string> Parameters { get; set; } = new();

    public DateTime ExpiresAtUtc { get; set; }
}
