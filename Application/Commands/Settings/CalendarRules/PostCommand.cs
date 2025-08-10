using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Commands.Settings.CalendarRules;

public record PostCommand(CalendarRuleResource model) : IRequest<Klacks.Api.Domain.Models.Settings.CalendarRule>;
