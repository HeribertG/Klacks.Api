// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingProposal(
    GroupingProposalKind Kind,
    Guid? GroupId,
    string? NewGroupKey,
    Guid? ClientId,
    Guid? ShiftId,
    GroupingFindingCode Cause);
