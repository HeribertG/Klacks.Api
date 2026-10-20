// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// An employee the schedule shows for a group together with the employment window the grid locks days by.
/// </summary>
/// <param name="ClientId">The employee</param>
/// <param name="Employment">Entry and exit of the employee; an open exit leaves the window open at the end</param>
public sealed record PlanningAgentEmployment(Guid ClientId, MembershipWindow Employment);
