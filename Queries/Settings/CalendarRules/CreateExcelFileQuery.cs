using Klacks_api.Resources;
using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Queries.Settings.CalendarRules;

public record CreateExcelFileQuery(CalendarRulesFilter Filter) : IRequest<HttpResultResource>;
