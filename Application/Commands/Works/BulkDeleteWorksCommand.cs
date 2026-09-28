// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Works;

public record BulkDeleteWorksCommand(BulkDeleteWorksRequest Request) : IRequest<BulkWorksResponse>;
