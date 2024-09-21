using Klacks.Api.Resources;
using Klacks.Api.Resources.Filter;
using MediatR;

namespace Klacks.Api.Queries.Settings.CalendarRules;

public record CreateExcelFileQuery(CalendarRulesFilter Filter) : IRequest<HttpResultResource>;
