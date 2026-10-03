// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>Whether a planning constraint vetoes a plan (Hard) or only adds a weighted penalty (Soft).</summary>
public enum PlanningConstraintSeverity
{
    Hard = 1,
    Soft = 2,
}
