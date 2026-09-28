// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Settings;

public class OpenRouteServiceSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openrouteservice.org/v2";

    public bool IsConfigured => !string.IsNullOrEmpty(ApiKey);
}
