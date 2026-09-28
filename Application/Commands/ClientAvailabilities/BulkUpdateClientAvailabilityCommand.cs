// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ClientAvailabilities;

public record BulkUpdateClientAvailabilityCommand(
    ClientAvailabilityBulkRequest Request) : IRequest<int>;
