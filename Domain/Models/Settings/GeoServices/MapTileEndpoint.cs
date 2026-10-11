// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Settings.GeoServices;

public record MapTileEndpoint(
    bool IsEnabled,
    string? UrlTemplate,
    IReadOnlyList<string> Subdomains,
    string Attribution,
    int MaxZoom,
    GeoServiceDisabledReason DisabledReason);
