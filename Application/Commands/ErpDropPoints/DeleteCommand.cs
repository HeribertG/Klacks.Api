// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ErpDropPoints;

public record DeleteCommand(Guid Id) : IRequest<ErpDropPointResource?>;
