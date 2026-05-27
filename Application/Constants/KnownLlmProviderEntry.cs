// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

public sealed record KnownLlmProviderEntry(
    string ProviderId,
    string ProviderName,
    string BaseUrl,
    bool RequiresApiKey,
    string DocsUrl,
    string? ApiVersion = null);
