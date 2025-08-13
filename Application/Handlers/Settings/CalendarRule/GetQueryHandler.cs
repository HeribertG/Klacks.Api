using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.CalendarRule?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;

        public GetQueryHandler(SettingsApplicationService settingsApplicationService)
        {
            _settingsApplicationService = settingsApplicationService;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await _settingsApplicationService.GetCalendarRuleByIdAsync(request.Id, cancellationToken);
        }
    }
}
