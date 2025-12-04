using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Settings;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.Settings?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ISettingsRepository settingsRepository, ILogger<GetQueryHandler> logger)
        {
            _settingsRepository = settingsRepository;
            _logger = logger;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Settings?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching setting with type: {request.Type}");
            
            try
            {
                var setting = await _settingsRepository.GetSetting(request.Type);
                
                _logger.LogInformation($"Successfully retrieved setting with type: {request.Type}");
                
                return setting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while fetching setting with type: {request.Type}");
                throw new InvalidRequestException($"Failed to retrieve setting: {ex.Message}");
            }
        }
    }
}
