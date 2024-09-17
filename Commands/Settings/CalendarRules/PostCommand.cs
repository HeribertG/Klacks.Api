using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Commands.Settings.CalendarRules;

public record PostCommand(CalendarRuleResource model) : IRequest<Models.Settings.CalendarRule>;
