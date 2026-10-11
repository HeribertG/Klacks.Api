// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Central outbound lock of the geo HTTP clients. A request only leaves the process when its scheme, host and
/// port belong to an enabled endpoint of one of the services this client serves; everything else gets a
/// synthetic 503 without any network access. Callers already fall back on non-success (Haversine, null,
/// ServiceUnavailable). A warning from here means a caller forgot to check IsEnabled. A redirect answer (3xx) of an
/// allowed endpoint is never followed and also becomes a synthetic 503, so a redirect cannot lead past the lock.
/// </summary>
/// <param name="policy">Effective geo service configuration</param>
/// <param name="kinds">Services whose endpoints this HTTP client may reach</param>
/// <param name="logger">Logs every blocked request (host only, never path or query)</param>

using System.Net;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Services.GeoServices;

namespace Klacks.Api.Infrastructure.Http;

public class GeoServiceOutboundGuardHandler : DelegatingHandler
{
    private const string BlockedReasonPhrase = "Blocked by geo service configuration";
    private const string ListSeparator = ",";
    private const int FirstRedirectStatusCode = 300;
    private const int LastRedirectStatusCode = 399;

    private readonly IGeoServicesPolicy _policy;
    private readonly IReadOnlyList<GeoServiceKind> _kinds;
    private readonly ILogger<GeoServiceOutboundGuardHandler> _logger;

    public GeoServiceOutboundGuardHandler(
        IGeoServicesPolicy policy,
        IReadOnlyList<GeoServiceKind> kinds,
        ILogger<GeoServiceOutboundGuardHandler> logger)
    {
        _policy = policy;
        _kinds = kinds;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var target = request.RequestUri;
        if (target == null || !target.IsAbsoluteUri || !IsAllowed(target))
        {
            _logger.LogWarning(
                "Blocked outbound geo request to {Host} ({Kind}: {Reason})",
                target?.IsAbsoluteUri == true ? target.Authority : string.Empty,
                string.Join(ListSeparator, _kinds),
                DescribeReasons());
            return Blocked(request);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (!IsRedirect(response.StatusCode))
        {
            return response;
        }

        _logger.LogWarning(
            "Refused to follow a redirect ({StatusCode}) from geo service host {Host} ({Kind})",
            (int)response.StatusCode,
            target.Authority,
            string.Join(ListSeparator, _kinds));
        response.Dispose();
        return Blocked(request);
    }

    private static HttpResponseMessage Blocked(HttpRequestMessage request)
    {
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            ReasonPhrase = BlockedReasonPhrase,
            RequestMessage = request
        };
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code >= FirstRedirectStatusCode && code <= LastRedirectStatusCode;
    }

    private bool IsAllowed(Uri target)
    {
        foreach (var kind in _kinds)
        {
            var endpoint = _policy.Get(kind);
            if (!endpoint.IsEnabled)
            {
                continue;
            }

            var allowed = GeoServicesPolicyEvaluator.TryParseHttpUrl(endpoint.BaseUrl);
            if (allowed != null
                && string.Equals(allowed.Scheme, target.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(allowed.Host, target.Host, StringComparison.OrdinalIgnoreCase)
                && allowed.Port == target.Port)
            {
                return true;
            }
        }

        return false;
    }

    private string DescribeReasons()
    {
        return string.Join(ListSeparator, _kinds.Select(kind => _policy.Get(kind).DisabledReason));
    }
}
