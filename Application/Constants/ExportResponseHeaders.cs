// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Response headers of the export download endpoints. They must be listed in the CORS policy's exposed headers,
/// otherwise the browser hides them from the SPA.
/// </summary>
public static class ExportResponseHeaders
{
    public const string SkippedEntries = "X-Klacks-Export-Skipped";

    public const string AbsenceMappingInvalid = "X-Klacks-Export-Mapping-Invalid";

    public const string Persons = "X-Klacks-Export-Persons";

    public const string Supplementary = "X-Klacks-Export-Supplementary";
}