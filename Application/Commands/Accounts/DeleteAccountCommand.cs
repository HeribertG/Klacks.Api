// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

public record DeleteAccountCommand(Guid UserId) : IRequest<HttpResultResource>;
