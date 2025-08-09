using Klacks.Api.Presentation.Resources.Settings;
using MediatR;

namespace Klacks.Api.Commands.Settings.CalendarRules;

public record PostCommand(CalendarRuleResource model) : IRequest<Models.Settings.CalendarRule>;
