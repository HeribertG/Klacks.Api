// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

/// <summary>
/// One period of one group that the close gates are evaluated for.
/// </summary>
/// <param name="Group">The group the period belongs to</param>
/// <param name="PeriodStart">First day of the period</param>
/// <param name="PeriodEnd">Last day of the period</param>
public sealed record PeriodCloseCandidate(Group Group, DateOnly PeriodStart, DateOnly PeriodEnd);
