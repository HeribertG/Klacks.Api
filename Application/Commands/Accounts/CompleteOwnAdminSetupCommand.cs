// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

public record CompleteOwnAdminSetupCommand(RegistrationResource NewAdmin) : IRequest<bool>;
