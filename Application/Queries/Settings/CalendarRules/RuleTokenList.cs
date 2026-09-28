// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record RuleTokenList(bool IsSelected) : IRequest<IEnumerable<Domain.DTOs.Filter.StateCountryToken>>;
