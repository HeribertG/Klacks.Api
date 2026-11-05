// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Request headers that prove a cookie-authenticated request was sent by the Klacks Ui and not forged by another site:
/// the X-Requested-With header the Ui sets on its requests (the Ui mirrors name and value) and the Fetch Metadata header
/// Sec-Fetch-Site, which the browser sets itself and a page cannot forge.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class CsrfProofConstants
{
    public const string RequestedWithHeader = "X-Requested-With";

    public const string RequestedWithValue = "XMLHttpRequest";

    public const string FetchSiteHeader = "Sec-Fetch-Site";

    public const string FetchSiteSameOrigin = "same-origin";
}
