// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.BreakPlaceholders;

public record ListQuery(BreakFilter Filter) : IRequest<(IEnumerable<ClientBreakPlaceholderResource> Clients, int TotalCount)>;
