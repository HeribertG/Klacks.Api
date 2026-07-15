// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What <see cref="EntityImportPlanner"/> decided to do with one desired entity-import row.
/// </summary>
/// <param name="Action">Insert, Update or SkipEdited (a customer edit was detected)</param>
/// <param name="SourceKey">Natural key of the row</param>
/// <param name="ContentHash">Hash to write as the row's new ImportContentHash on Insert/Update</param>
/// <param name="Values">Value payload to write on Insert/Update; unused for SkipEdited</param>

namespace Klacks.Api.Application.Services.Setup;

public sealed record EntityImportDecision<TValues>(EntityImportAction Action, string SourceKey, string ContentHash, TValues Values);
