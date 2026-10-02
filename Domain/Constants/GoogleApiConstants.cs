// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants shared by every client of a Google API that authenticates with an API key.
/// The key travels in a request header so it never becomes part of a request URI, where loggers,
/// exception messages and proxies would see it.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class GoogleApiConstants
{
    public const string ApiKeyHeaderName = "x-goog-api-key";
}
