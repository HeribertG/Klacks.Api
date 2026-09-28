// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public record GroupLocationResolveResult(
    Guid GroupId,
    string GroupName,
    GroupLocationResolveOutcome Outcome,
    double? Latitude,
    double? Longitude);
