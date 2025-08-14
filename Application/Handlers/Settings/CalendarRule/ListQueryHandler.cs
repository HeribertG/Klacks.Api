using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>>
    {
        private readonly ISettingsRepository _settingsRepository;

        public ListQueryHandler(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            return await _settingsRepository.GetCalendarRuleList();
        }
    }
}
