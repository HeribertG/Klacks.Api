// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Settings;

public class OpenRouteServiceSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openrouteservice.org/v2";

    public bool IsConfigured => !string.IsNullOrEmpty(ApiKey);
}
