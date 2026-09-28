// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingGroupRecord(
    Guid Id,
    string Name,
    Guid? ParentId,
    Guid? RootId,
    double? Latitude,
    double? Longitude);
