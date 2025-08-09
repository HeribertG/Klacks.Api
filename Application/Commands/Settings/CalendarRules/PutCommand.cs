using MediatR;

namespace Klacks.Api.Application.Commands.Settings.CalendarRules;

public record PutCommand(Models.Settings.CalendarRule model) : IRequest<Models.Settings.CalendarRule>;
