// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of resolving a language pack geo entry against the database.
/// </summary>
/// <param name="Row">Row the entry belongs to, or null when the entry has to be inserted</param>
/// <param name="IdTaken">True when the pack's id is already used by a row, so an insert needs a fresh id</param>

namespace Klacks.Api.Infrastructure.Services.Settings;

public sealed record LanguagePluginGeoRowMatch<T>(T? Row, bool IdTaken)
    where T : class
{
    public Guid IdForInsert(Guid packId) => IdTaken ? Guid.NewGuid() : packId;
}
