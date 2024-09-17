using MediatR;

namespace Klacks_api.Commands.Settings.CalendarRules;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.CalendarRule>;
