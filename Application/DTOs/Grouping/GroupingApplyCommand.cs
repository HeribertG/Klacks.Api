// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingApplyCommand(
    IReadOnlyList<GroupingProposal> Proposals,
    DateTime ValidFromUtc,
    string? NewGroupName,
    string UserName);
