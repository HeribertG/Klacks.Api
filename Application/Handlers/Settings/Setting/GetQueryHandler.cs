using Klacks.Api.Application.Services;
using Klacks.Api.Application.Queries.Settings.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.Settings?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;

        public GetQueryHandler(SettingsApplicationService settingsApplicationService)
        {
            _settingsApplicationService = settingsApplicationService;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Settings?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await _settingsApplicationService.GetSettingByTypeAsync(request.Type, cancellationToken);
        }
    }
}
