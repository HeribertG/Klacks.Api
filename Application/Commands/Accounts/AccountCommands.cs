// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

public record RegisterUserCommand(RegistrationResource Registration) : IRequest<AuthenticatedResult>;

public record ChangePasswordCommand(ChangePasswordResource ChangePassword) : IRequest<AuthenticatedResult>;

public record ChangePasswordUserCommand(ChangePasswordResource ChangePassword) : IRequest<AuthenticatedResult>;

public record ChangeRoleCommand(ChangeRole ChangeRole) : IRequest<HttpResultResource>;

public record DeleteAccountCommand(Guid UserId) : IRequest<HttpResultResource>;

public record RequestPasswordResetCommand(string Email) : IRequest<HttpResultResource>;

public record ResetPasswordCommand(ResetPasswordResource ResetPassword) : IRequest<AuthenticatedResult>;

public record UpdateAccountCommand(UpdateAccountResource UpdateAccount) : IRequest<HttpResultResource>;