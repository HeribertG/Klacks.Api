// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingAddressRow(Guid ClientId, DateTime? ValidFrom, double? Latitude, double? Longitude);
