// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

public record RevokeDayApprovalCommand(DateOnly Date, Guid GroupId) : IRequest<int>;
