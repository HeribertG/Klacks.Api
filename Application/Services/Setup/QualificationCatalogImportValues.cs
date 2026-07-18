// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired Qualification catalog row for the region-setup entity import (K20).
/// The four core-language names are reconciled as a group (a language absent from the file clears the
/// stored value); Category is derived from the industry slug of the surrounding profile block.
/// Industry is classification derived from the block's industry slug, not editable content: it is
/// re-applied on Insert AND Update but deliberately excluded from ComputeQualificationContentHash,
/// so rows written by an older binary (Industry empty) are never misread as customer-edited.
/// </summary>
public sealed record QualificationCatalogImportValues(
    string? NameDe,
    string? NameEn,
    string? NameFr,
    string? NameIt,
    bool IsTimeLimited,
    QualificationCategory Category,
    string Industry);
