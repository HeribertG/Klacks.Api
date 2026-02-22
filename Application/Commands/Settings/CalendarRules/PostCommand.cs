// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.CalendarRules;

public record PostCommand(CalendarRuleResource model) : IRequest<Klacks.Api.Domain.Models.Settings.CalendarRule>;
