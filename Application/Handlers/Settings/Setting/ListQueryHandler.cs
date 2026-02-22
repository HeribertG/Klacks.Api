// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Settings;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISettingsEncryptionService _encryptionService;
        private readonly ILogger<ListQueryHandler> _logger;

        public ListQueryHandler(
            ISettingsRepository settingsRepository,
            ISettingsEncryptionService encryptionService,
            ILogger<ListQueryHandler> logger)
        {
            _settingsRepository = settingsRepository;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching settings list");

            try
            {
                var settings = await _settingsRepository.GetSettingsList();
                var settingsList = settings.ToList();

                foreach (var setting in settingsList)
                {
                    var originalValue = setting.Value;
                    setting.Value = _encryptionService.ProcessForReading(setting.Type, setting.Value);

                    if (_encryptionService.IsSensitiveSettingType(setting.Type))
                    {
                        _logger.LogInformation("Setting {Type}: Original starts with ENC: {IsEncrypted}, Decrypted length: {Length}",
                            setting.Type,
                            originalValue?.StartsWith("ENC:") ?? false,
                            setting.Value?.Length ?? 0);
                    }
                }

                _logger.LogInformation("Retrieved {Count} settings", settingsList.Count);

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
