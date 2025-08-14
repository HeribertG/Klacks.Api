using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.CalendarRule?>
    {
        private readonly ISettingsRepository _settingsRepository;

        public GetQueryHandler(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await _settingsRepository.GetCalendarRule(request.Id);
        }
    }
}
