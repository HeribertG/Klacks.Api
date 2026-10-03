// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>Who brought a planning rule (CounterRule or PlanningConstraint) into the system. Zero is deliberately unused so an unset value never collides with a database default.</summary>
public enum RuleOrigin
{
    Admin = 1,
    LlmProposal = 2,
    EmployeeRequest = 3,
    Import = 4,
}
