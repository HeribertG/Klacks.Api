using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Settings;
using Klacks.Api.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ILogger<ListQueryHandler> _logger;

        public ListQueryHandler(ISettingsRepository settingsRepository, ILogger<ListQueryHandler> logger)
        {
            _settingsRepository = settingsRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching settings list");
            
            try
            {
                var settings = await _settingsRepository.GetSettingsList();
                var settingsList = settings.ToList();
                
                _logger.LogInformation($"Retrieved {settingsList.Count} settings");
                
                return settingsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching settings list");
                throw new InvalidRequestException($"Failed to retrieve settings list: {ex.Message}");
            }
        }
    }
}
