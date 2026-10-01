// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Groups;

/// <summary>
/// Not-found messages of the group-link commands; a group the caller cannot see uses the same text as a group
/// that does not exist.
/// </summary>
public static class GroupItemVisibilityMessages
{
    public const string GroupNotFound = "Group with ID {0} not found";
}
