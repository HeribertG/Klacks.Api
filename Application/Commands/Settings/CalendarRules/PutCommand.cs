using MediatR;

namespace Klacks.Api.Application.Commands.Settings.CalendarRules;

public record PutCommand(Klacks.Api.Domain.Models.Settings.CalendarRule model) : IRequest<Klacks.Api.Domain.Models.Settings.CalendarRule>;
