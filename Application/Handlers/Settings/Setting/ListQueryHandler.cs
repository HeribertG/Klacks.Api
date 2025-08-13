using Klacks.Api.Application.Services;
using Klacks.Api.Application.Queries.Settings.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>>
    {
        private readonly SettingsApplicationService _settingsApplicationService;

        public ListQueryHandler(SettingsApplicationService settingsApplicationService)
        {
            _settingsApplicationService = settingsApplicationService;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> Handle(ListQuery request, CancellationToken cancellationToken) => await _settingsApplicationService.GetAllSettingsAsync(cancellationToken);
    }
}
