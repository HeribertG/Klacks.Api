// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Sign-in on the OAuth authorize page that issues an authorization code for an external client.
/// </summary>
/// <param name="IsLocalRequest">The request comes straight from the machine itself, without a proxy</param>

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.OAuth;

public record IssueOAuthAuthorizationCodeCommand(
    string Email,
    string Password,
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    string? Scope,
    bool IsLocalRequest) : IRequest<OAuthAuthorizationCodeResult>;
