// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Security headers of the consent page: it must not be framed (Content-Security-Policy frame-ancestors and
/// X-Frame-Options), not cached and not leak its address through the Referer header. The headers are assigned, not
/// appended, so they replace what the anti-forgery service already wrote (it sets X-Frame-Options SAMEORIGIN and a
/// Cache-Control of its own) and no header is sent twice. There is deliberately no form-action directive: the form
/// answer is a redirect to the tool's own address, which form-action would block in some browsers.
/// </summary>
/// <param name="response">Response of the consent page</param>

using Microsoft.Net.Http.Headers;

namespace Klacks.Api.Presentation.Controllers.OAuth;

public static class OAuthConsentPageHeaders
{
    public const string ContentSecurityPolicyValue = "frame-ancestors 'none'";

    public const string XFrameOptionsName = "X-Frame-Options";

    public const string XFrameOptionsValue = "DENY";

    public const string CacheControlValue = "no-store";

    public const string ReferrerPolicyName = "Referrer-Policy";

    public const string ReferrerPolicyValue = "no-referrer";

    public static void Apply(HttpResponse response)
    {
        response.Headers[HeaderNames.ContentSecurityPolicy] = ContentSecurityPolicyValue;
        response.Headers[XFrameOptionsName] = XFrameOptionsValue;
        response.Headers[HeaderNames.CacheControl] = CacheControlValue;
        response.Headers[ReferrerPolicyName] = ReferrerPolicyValue;
    }
}
