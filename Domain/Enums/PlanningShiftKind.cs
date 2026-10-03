// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>Day kind a sequence constraint matches on (stored by name in ParametersJson); mirrors the optimizer RuleShiftKind.</summary>
public enum PlanningShiftKind
{
    Work = 1,
    Early = 2,
    Late = 3,
    Night = 4,
}
