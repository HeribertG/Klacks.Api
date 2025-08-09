using MediatR;

namespace Klacks.Api.Commands.Settings.CalendarRules;

public record PutCommand(Models.Settings.CalendarRule model) : IRequest<Models.Settings.CalendarRule>;
