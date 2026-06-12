// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler producing the RFC 8414 authorization server metadata document for the built-in
/// OAuth 2.1 authorization server, derived from the configured public MCP base URL.
/// </summary>
/// <param name="request">Marker query without parameters</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Application.Queries.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Handlers.OAuth;

public class GetAuthorizationServerMetadataQueryHandler : IRequestHandler<GetAuthorizationServerMetadataQuery, AuthorizationServerMetadataResource>
{
    private readonly IOptions<McpPublicEndpointOptions> _options;

    public GetAuthorizationServerMetadataQueryHandler(IOptions<McpPublicEndpointOptions> options)
    {
        _options = options;
    }

    public Task<AuthorizationServerMetadataResource> Handle(GetAuthorizationServerMetadataQuery request, CancellationToken cancellationToken)
    {
        var baseUrl = _options.Value.NormalizedPublicBaseUrl;

        var metadata = new AuthorizationServerMetadataResource(
            Issuer: baseUrl,
            AuthorizationEndpoint: $"{baseUrl}/{OAuthConstants.RouteBase}/{OAuthConstants.AuthorizeEndpointName}",
            TokenEndpoint: $"{baseUrl}/{OAuthConstants.RouteBase}/{OAuthConstants.TokenEndpointName}",
            RegistrationEndpoint: $"{baseUrl}/{OAuthConstants.RouteBase}/{OAuthConstants.RegisterEndpointName}",
            ResponseTypesSupported: [OAuthConstants.ResponseTypeCode],
            GrantTypesSupported: [OAuthConstants.GrantTypeAuthorizationCode],
            CodeChallengeMethodsSupported: [OAuthConstants.CodeChallengeMethodS256],
            TokenEndpointAuthMethodsSupported: [OAuthConstants.TokenEndpointAuthMethodNone],
            ScopesSupported: [OAuthConstants.McpToolsScope]);

        return Task.FromResult(metadata);
    }
}
