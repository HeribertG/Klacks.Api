// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats the shift resolution and the booking diagnosis share when they write dates and open ends into skill text.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Skills;

internal static class SkillDisplayFormats
{
    public const string IsoDate = DateTimeFormats.IsoDate;

    public const string OpenEnd = "open";
}
