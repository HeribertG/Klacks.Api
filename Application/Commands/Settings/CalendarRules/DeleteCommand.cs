using MediatR;

namespace Klacks.Api.Application.Commands.Settings.CalendarRules;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.CalendarRule>;
