// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating or upserting a global setting. Runs key-specific value validation
/// (ISettingValueValidator) before persisting, so invalid values for keys with a registered rule
/// (e.g. ACTIVE_INDUSTRIES) never reach the settings store.
/// Writing one of the incoming-server keys can make the inbox page appear or disappear, so the
/// navigation target snapshot is marked stale afterwards — without it the chat fast-path would keep
/// the old answer for up to its TTL.
/// </summary>
/// <param name="request">Contains the setting with its key (Type) and the new value</param>

using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Klacksy;
using InboxSettingKeys = Klacks.Api.Application.Constants.InboxAvailabilitySettingKeys;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.Settings?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISettingsEncryptionService _encryptionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISettingValueValidator _settingValueValidator;
        private readonly INavigationTargetCacheService _navigationTargetCache;

        public PostCommandHandler(
            ISettingsRepository settingsRepository,
            ISettingsEncryptionService encryptionService,
            IUnitOfWork unitOfWork,
            ISettingValueValidator settingValueValidator,
            INavigationTargetCacheService navigationTargetCache,
            ILogger<PostCommandHandler> logger)
            : base(logger)
        {
            _settingsRepository = settingsRepository;
            _encryptionService = encryptionService;
            _unitOfWork = unitOfWork;
            _settingValueValidator = settingValueValidator;
            _navigationTargetCache = navigationTargetCache;
        }

        public async Task<Domain.Models.Settings.Settings?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            if (_encryptionService.IsServerOnlySettingType(request.model.Type) && request.model.Value == SettingsMasking.MaskedValue)
            {
                return await _settingsRepository.GetSetting(request.model.Type);
            }

            _settingValueValidator.Validate(request.model.Type, request.model.Value);

            request.model.Value = _encryptionService.ProcessForStorage(request.model.Type, request.model.Value);

            var existingSetting = await _settingsRepository.GetSetting(request.model.Type);
            if (existingSetting != null)
            {
                existingSetting.Value = request.model.Value;
                await _settingsRepository.PutSetting(existingSetting);
                await _unitOfWork.CompleteAsync();
                InvalidateNavigationTargetCacheIfInboxRelevant(request.model.Type);
                return existingSetting;
            }

            var res = await _settingsRepository.AddSetting(request.model);
            await _unitOfWork.CompleteAsync();
            InvalidateNavigationTargetCacheIfInboxRelevant(request.model.Type);
            return res;
        }

        private void InvalidateNavigationTargetCacheIfInboxRelevant(string settingKey)
        {
            if (!InboxSettingKeys.All.Contains(settingKey))
            {
                return;
            }

            try
            {
                _navigationTargetCache.Invalidate();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Post-commit invalidation of the navigation target cache failed for setting {SettingKey}; the settings update is persisted and remains unaffected.",
                    settingKey);
            }
        }
    }
}
