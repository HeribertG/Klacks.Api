// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decision an <see cref="EntityImportPlanner"/> reaches for one desired entity-import row.
/// </summary>

namespace Klacks.Api.Application.Services.Setup;

public enum EntityImportAction
{
    Insert,
    Update,
    SkipEdited,
}
