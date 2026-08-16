// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

/// <summary>
/// Bars an account from signing in without destroying it, the reversible counterpart of
/// DeleteAccountCommand.
/// </summary>
/// <param name="UserId">Identifier of the account to deactivate.</param>
public record DeactivateAccountCommand(Guid UserId) : IRequest<HttpResultResource>;
