// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

/// <summary>
/// Invalidates all server-side refresh tokens of the given user, so a stolen refresh token
/// cannot be used to obtain new access tokens after the user has logged out.
/// </summary>
/// <param name="UserId">Id of the currently authenticated user (read from the JWT claims)</param>
public record LogoutCommand(string UserId) : IRequest;
