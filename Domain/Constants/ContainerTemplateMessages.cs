// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Not-found texts of the container template and override writes, so an item or override of another template or
/// container is answered exactly like a missing one.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContainerTemplateMessages
{
    public const string ItemNotFound = "ContainerTemplateItem with ID {0} not found";

    public const string OverrideNotFound = "ContainerShiftOverride {0} not found.";
}
