// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record ListQuery : IRequest<IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>>;
