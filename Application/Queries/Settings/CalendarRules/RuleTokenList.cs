// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record RuleTokenList(bool IsSelected) : IRequest<IEnumerable<Application.DTOs.Filter.StateCountryToken>>;
