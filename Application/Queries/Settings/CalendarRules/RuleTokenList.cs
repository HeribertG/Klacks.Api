using MediatR;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record RuleTokenList(bool IsSelected) : IRequest<IEnumerable<Presentation.DTOs.Filter.StateCountryToken>>;
