// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating or upserting a global setting. Runs key-specific value validation
/// (ISettingValueValidator) before persisting, so invalid values for keys with a registered rule
/// (e.g. ACTIVE_INDUSTRIES) never reach the settings store.
/// </summary>
/// <param name="request">Contains the setting with its key (Type) and the new value</param>

using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.Settings?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISettingsEncryptionService _encryptionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISettingValueValidator _settingValueValidator;

        public PostCommandHandler(
            ISettingsRepository settingsRepository,
            ISettingsEncryptionService encryptionService,
            IUnitOfWork unitOfWork,
            ISettingValueValidator settingValueValidator,
            ILogger<PostCommandHandler> logger)
            : base(logger)
        {
            _settingsRepository = settingsRepository;
            _encryptionService = encryptionService;
            _unitOfWork = unitOfWork;
            _settingValueValidator = settingValueValidator;
        }

        public async Task<Domain.Models.Settings.Settings?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            if (_encryptionService.IsServerOnlySettingType(request.model.Type) && request.model.Value == "***")
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
                return existingSetting;
            }

            var res = await _settingsRepository.AddSetting(request.model);
            await _unitOfWork.CompleteAsync();
            return res;
        }
    }
}
