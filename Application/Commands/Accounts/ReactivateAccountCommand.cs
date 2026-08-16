// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

/// <summary>
/// Undoes a deactivation and lets the account sign in again.
/// </summary>
/// <param name="UserId">Identifier of the account to reactivate.</param>
public record ReactivateAccountCommand(Guid UserId) : IRequest<HttpResultResource>;
