// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Sign-in with e-mail and password.
/// </summary>
/// <param name="Email">E-mail address typed on the sign-in page</param>
/// <param name="Password">Password typed on the sign-in page</param>
/// <param name="IsLocalRequest">The request comes straight from the machine itself, without a proxy</param>

using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Accounts;

public record LoginUserQuery(string Email, string Password, bool IsLocalRequest) : IRequest<TokenResource>;