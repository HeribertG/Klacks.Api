// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a global setting. Runs key-specific value validation
/// (ISettingValueValidator) before persisting, so invalid values for keys with a registered rule
/// (e.g. ACTIVE_INDUSTRIES) never reach the settings store. After the update is committed it
/// raises a SurchargeSettingsChangedEvent when the setting key is surcharge-relevant (see
/// SurchargeRelevantSettingKeys) and the stored value actually changed, so persisted work
/// surcharges are recalculated. The dispatch is post-commit and defensive: a failure is logged
/// and never affects the committed settings update.
/// </summary>
/// <param name="request">Contains the setting with its key (Type) and the new value</param>

using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, Domain.Models.Settings.Settings?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISettingsEncryptionService _encryptionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDomainEventDispatcher _eventDispatcher;
        private readonly ISettingValueValidator _settingValueValidator;

        public PutCommandHandler(
            ISettingsRepository settingsRepository,
            ISettingsEncryptionService encryptionService,
            IUnitOfWork unitOfWork,
            IDomainEventDispatcher eventDispatcher,
            ISettingValueValidator settingValueValidator,
            ILogger<PutCommandHandler> logger)
            : base(logger)
        {
            _settingsRepository = settingsRepository;
            _encryptionService = encryptionService;
            _unitOfWork = unitOfWork;
            _eventDispatcher = eventDispatcher;
            _settingValueValidator = settingValueValidator;
        }

        public async Task<Domain.Models.Settings.Settings?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            if (_encryptionService.IsServerOnlySettingType(request.model.Type) && request.model.Value == SettingsMasking.MaskedValue)
            {
                return await _settingsRepository.GetSetting(request.model.Type);
            }

            _settingValueValidator.Validate(request.model.Type, request.model.Value);

            request.model.Value = _encryptionService.ProcessForStorage(request.model.Type, request.model.Value);

            var tracksPreviousValue = SurchargeRelevantSettingKeys.All.Contains(request.model.Type)
                || request.model.Type == SettingKeys.ActiveIndustries;

            var previousValue = tracksPreviousValue
                ? (await _settingsRepository.GetSettingNoTracking(request.model.Type))?.Value
                : null;

            var res = await _settingsRepository.PutSetting(request.model);
            await _unitOfWork.CompleteAsync();

            var valueChanged = !string.Equals(previousValue, request.model.Value, StringComparison.Ordinal);

            if (SurchargeRelevantSettingKeys.All.Contains(request.model.Type) && valueChanged)
            {
                await DispatchSurchargeSettingsChangedAsync(request.model.Type);
            }

            if (request.model.Type == SettingKeys.ActiveIndustries && valueChanged)
            {
                await DispatchActiveIndustriesChangedAsync(previousValue, request.model.Value);
            }

            return res;
        }

        private async Task DispatchSurchargeSettingsChangedAsync(string settingKey)
        {
            try
            {
                await _eventDispatcher.DispatchAsync(
                    new SurchargeSettingsChangedEvent([settingKey]),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Post-commit dispatch of {EventName} failed for setting {SettingKey}; the settings update is persisted and remains unaffected.",
                    nameof(SurchargeSettingsChangedEvent),
                    settingKey);
            }
        }

        private async Task DispatchActiveIndustriesChangedAsync(string? previousValue, string currentValue)
        {
            try
            {
                await _eventDispatcher.DispatchAsync(
                    new ActiveIndustriesChangedEvent(previousValue, currentValue),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Post-commit dispatch of {EventName} failed; the settings update is persisted and remains unaffected.",
                    nameof(ActiveIndustriesChangedEvent));
            }
        }
    }
}
