// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Display metadata of a qualification (localized name + optional emoji) carried alongside the
/// eligibility matrix so the gap report can be rendered without a second database round-trip.
/// </summary>
/// <param name="Name">Localized qualification name</param>
/// <param name="Emoji">Optional emoji glyph</param>
public sealed record QualificationInfo(MultiLanguage Name, string? Emoji);
