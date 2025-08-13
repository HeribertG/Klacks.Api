using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;

namespace Klacks.Api.Application.Commands.Accounts;

// Register User Command
public record RegisterUserCommand(RegistrationResource Registration) : IRequest<AuthenticatedResult>;

// Change Password Command
public record ChangePasswordCommand(ChangePasswordResource ChangePassword) : IRequest<AuthenticatedResult>;

// Change Password User Command
public record ChangePasswordUserCommand(ChangePasswordResource ChangePassword) : IRequest<AuthenticatedResult>;

// Change Role Command
public record ChangeRoleCommand(ChangeRole ChangeRole) : IRequest<HttpResultResource>;

// Delete Account Command
public record DeleteAccountCommand(Guid UserId) : IRequest<HttpResultResource>;