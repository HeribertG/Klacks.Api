// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ClientAvailabilities;

public record BulkUpdateClientAvailabilityCommand(
    ClientAvailabilityBulkRequest Request) : IRequest<int>;
