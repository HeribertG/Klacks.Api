// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record ValidateRuleQuery(string Rule, string? SubRule, int? Year) : IRequest<ValidateCalendarRuleResponse>;
