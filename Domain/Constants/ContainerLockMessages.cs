// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rejection texts of the container edit lock endpoint and of the container work save it guards; a hidden work is
/// answered with exactly the text of a missing one.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContainerLockMessages
{
    public const string UnknownResourceType = "Unknown container lock resource type '{0}'";

    public const string WorkNotFound = "Work with ID {0} not found.";
}
