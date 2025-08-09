using MediatR;

namespace Klacks.Api.Commands.Settings.CalendarRules;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.CalendarRule>;
