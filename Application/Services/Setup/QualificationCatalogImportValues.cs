// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired Qualification catalog row for the region-setup entity import (K20).
/// The four core-language names are reconciled as a group (a language absent from the file clears the
/// stored value); Category is derived from the industry slug of the surrounding profile block.
/// </summary>
public sealed record QualificationCatalogImportValues(
    string? NameDe,
    string? NameEn,
    string? NameFr,
    string? NameIt,
    bool IsTimeLimited,
    QualificationCategory Category);
