// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Associations;

/// <summary>
/// Id, name and abbreviation of a shift that is linked to a group, the three columns the shift-preference picker shows.
/// </summary>
/// <param name="Id">Shift id</param>
/// <param name="Name">Shift name</param>
/// <param name="Abbreviation">Shift abbreviation</param>
public sealed record LinkedShiftName(Guid Id, string Name, string Abbreviation);
