// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.OAuth;

public record IssueOAuthAuthorizationCodeCommand(
    string Email,
    string Password,
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    string? Scope) : IRequest<OAuthAuthorizationCodeResult>;
