// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Update;

public record RequestRollbackCommand(string RequestedBy) : IRequest<UpdateTriggerResult>;
