// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Fixed row ids of absence types shipped by AbsencesSeed that code refers to. A customer may still rename, change or
/// delete these rows, so callers check that the row exists and still fits before using it.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SeededAbsenceIds
{
    public static readonly Guid UnpaidBreak = Guid.Parse("b8e3c5a1-2d4f-4e6b-9c8a-1f2e3d4c5b6a");
}
