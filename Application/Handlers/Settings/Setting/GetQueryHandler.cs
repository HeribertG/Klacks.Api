using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.Settings?>
    {
        private readonly ISettingsRepository _settingsRepository;

        public GetQueryHandler(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Settings?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await _settingsRepository.GetSetting(request.Type);
        }
    }
}
