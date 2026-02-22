// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record ListQuery : IRequest<IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>>;
