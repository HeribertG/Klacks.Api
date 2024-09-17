using MediatR;

namespace Klacks_api.Commands.Settings.CalendarRules;

public record PutCommand(Models.Settings.CalendarRule model) : IRequest<Models.Settings.CalendarRule>;
