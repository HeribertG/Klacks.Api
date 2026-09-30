// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Grouping;

/// <summary>
/// Moves every sealed order, plannable shift and split shift that is not yet linked to a city group
/// (a leaf of the group tree) into the city group its customer's address points to, replacing its
/// current group links. With Apply=false it only previews the plan.
/// </summary>
/// <param name="CustomerName">Case-insensitive fragment the customer's name or company must contain; null for every customer.</param>
/// <param name="MaxCount">Upper bound on the number of shifts processed; null processes every match.</param>
/// <param name="Apply">False for a dry-run preview, true to persist the moves.</param>
/// <param name="UserName">Name of the acting user, stored on the created links.</param>
public record AssignShiftsToCityGroupsCommand(
    string? CustomerName,
    int? MaxCount,
    bool Apply,
    string UserName) : IRequest<AssignShiftsToCityGroupsResult>;
