using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>>
    {
        private readonly SettingsApplicationService _settingsApplicationService;

        public ListQueryHandler(SettingsApplicationService settingsApplicationService)
        {
            _settingsApplicationService = settingsApplicationService;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            return await _settingsApplicationService.GetAllCalendarRulesAsync(cancellationToken);
        }
    }
}
