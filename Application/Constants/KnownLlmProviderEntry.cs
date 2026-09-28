// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

public sealed record KnownLlmProviderEntry(
    string ProviderId,
    string ProviderName,
    string BaseUrl,
    bool RequiresApiKey,
    string DocsUrl,
    string? ApiVersion = null);
