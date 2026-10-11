// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Settings.GeoServices;

public record GeoServiceEndpoint(bool IsEnabled, string? BaseUrl, GeoServiceDisabledReason DisabledReason);
