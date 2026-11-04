// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

/// <summary>
/// Changes the password of the signed-in caller.
/// </summary>
/// <param name="UserId">Id of the caller, taken from the access token; the e-mail in the body is not used to pick the account</param>
/// <param name="ChangePassword">Old and new password plus the confirmation mail title and text</param>
public record ChangePasswordUserCommand(string UserId, ChangePasswordResource ChangePassword) : IRequest<AuthenticatedResult>;
