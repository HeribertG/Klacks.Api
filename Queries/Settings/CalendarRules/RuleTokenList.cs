using MediatR;

namespace Klacks_api.Queries.Settings.CalendarRules;

public record RuleTokenList(bool IsSelected) : IRequest<IEnumerable<Resources.Filter.StateCountryToken>>;
