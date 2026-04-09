// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Presentation.Attributes;

/// <summary>
/// Marks a skill handler as wrapping a specific HTTP endpoint so the retrieval pipeline
/// can prefer the skill over the raw endpoint in search results.
/// </summary>
/// <param name="httpMethod">HTTP method of the wrapped endpoint (e.g. GET, POST). Normalized to uppercase.</param>
/// <param name="routeTemplate">ASP.NET route template of the wrapped endpoint (e.g. /api/backend/shifts/{id}).</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExposesEndpointAttribute : Attribute
{
    public string HttpMethod { get; }
    public string RouteTemplate { get; }
    public string EndpointKey => $"{HttpMethod} {RouteTemplate}";

    public ExposesEndpointAttribute(string httpMethod, string routeTemplate)
    {
        HttpMethod = httpMethod.ToUpperInvariant();
        RouteTemplate = routeTemplate;
    }
}
