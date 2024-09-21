using MediatR;

namespace Klacks.Api.Queries.Settings.CalendarRules;

public record RuleTokenList(bool IsSelected) : IRequest<IEnumerable<Resources.Filter.StateCountryToken>>;
