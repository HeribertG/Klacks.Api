// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// How the scheduling rules of the members of the executing group judge one part length of a split order: allowed for
/// every member, only for some of them, for none, or unknown because the group has no members.
/// </summary>
public enum OrderSplitVerdictKind
{
    Allowed = 0,
    AllowedForSome = 1,
    NotAllowed = 2,
    NoMembers = 3,
}
