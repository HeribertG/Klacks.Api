using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Commands.Settings.CalendarRules;

public record PostCommand(CalendarRuleResource model) : IRequest<Models.Settings.CalendarRule>;
