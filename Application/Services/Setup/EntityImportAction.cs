// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
