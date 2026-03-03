// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Works;

public record UnconfirmWorkCommand(Guid WorkId) : IRequest<WorkResource?>;
