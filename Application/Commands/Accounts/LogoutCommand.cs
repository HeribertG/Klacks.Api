// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

/// <summary>
/// Logs out this device: removes only the presented refresh token. The user's other devices stay signed in; signing out
/// everywhere happens through a password change, a password reset or a deactivation.
/// </summary>
/// <param name="RefreshToken">Raw refresh token of this device (cookie, else body), or null when the request carried none</param>
public record LogoutCommand(string? RefreshToken) : IRequest;
