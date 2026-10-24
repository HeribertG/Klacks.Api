// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// How the scheduling rules of the executing group's members judge one part length of a split.
/// </summary>
/// <param name="Kind">Allowed for all members, for some, for none, or no members to judge by</param>
/// <param name="AllowedMembers">Members whose rules allow a part of this length</param>
/// <param name="MemberCount">Members whose rules were checked</param>
/// <param name="PartHours">Length of one part in hours</param>
public sealed record OrderSplitVerdict(OrderSplitVerdictKind Kind, int AllowedMembers, int MemberCount, decimal PartHours);
