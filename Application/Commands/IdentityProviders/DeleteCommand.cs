// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Commands.IdentityProviders;

public record DeleteCommand(Guid Id) : IRequest<IdentityProviderResource?>;
