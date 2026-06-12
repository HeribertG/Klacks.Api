// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the built-in OAuth 2.1 authorization server that issues Klacks personal
/// access tokens to MCP clients (RFC 8414 metadata, RFC 7591 registration, PKCE flow).
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class OAuthConstants
{
    public const string WellKnownSegment = ".well-known";

    public const string AuthorizationServerMetadataEndpointName = "oauth-authorization-server";

    public const string RouteBase = "oauth";

    public const string AuthorizeEndpointName = "authorize";

    public const string TokenEndpointName = "token";

    public const string RegisterEndpointName = "register";

    public const string ResponseTypeCode = "code";

    public const string GrantTypeAuthorizationCode = "authorization_code";

    public const string CodeChallengeMethodS256 = "S256";

    public const string TokenEndpointAuthMethodNone = "none";

    public const string TokenTypeBearer = "Bearer";

    public const string McpToolsScope = "mcp:tools";

    public const string ParameterClientId = "client_id";

    public const string ParameterRedirectUri = "redirect_uri";

    public const string ParameterResponseType = "response_type";

    public const string ParameterCodeChallenge = "code_challenge";

    public const string ParameterCodeChallengeMethod = "code_challenge_method";

    public const string ParameterScope = "scope";

    public const string ParameterState = "state";

    public const string ParameterCode = "code";

    public const string ParameterGrantType = "grant_type";

    public const string ParameterCodeVerifier = "code_verifier";

    public const string ParameterError = "error";

    public const string ParameterErrorDescription = "error_description";

    public const string ParameterEmail = "email";

    public const string ParameterPassword = "password";

    public const string ParameterDecision = "decision";

    public const string DecisionApprove = "approve";

    public const string DecisionDeny = "deny";

    public const string ErrorInvalidRequest = "invalid_request";

    public const string ErrorInvalidClient = "invalid_client";

    public const string ErrorInvalidGrant = "invalid_grant";

    public const string ErrorUnsupportedResponseType = "unsupported_response_type";

    public const string ErrorUnsupportedGrantType = "unsupported_grant_type";

    public const string ErrorAccessDenied = "access_denied";

    public const string ErrorInvalidRedirectUri = "invalid_redirect_uri";

    public const string ErrorInvalidClientMetadata = "invalid_client_metadata";

    public const int AuthorizationCodeByteLength = 32;

    public const int AccessTokenExpiresInDays = 30;

    public const string AccessTokenNamePrefix = "oauth:";

    public const string LoopbackHostLocalhost = "localhost";

    public static readonly TimeSpan AuthorizationCodeLifetime = TimeSpan.FromMinutes(5);
}
