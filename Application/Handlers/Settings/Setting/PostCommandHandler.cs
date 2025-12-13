using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.Settings?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISettingsEncryptionService _encryptionService;
        private readonly IUnitOfWork _unitOfWork;

        public PostCommandHandler(
            ISettingsRepository settingsRepository,
            ISettingsEncryptionService encryptionService,
            IUnitOfWork unitOfWork,
            ILogger<PostCommandHandler> logger)
            : base(logger)
        {
            _settingsRepository = settingsRepository;
            _encryptionService = encryptionService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.Settings?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            request.model.Value = _encryptionService.ProcessForStorage(request.model.Type, request.model.Value);
            var res = await _settingsRepository.AddSetting(request.model);
            await _unitOfWork.CompleteAsync();
            return res;
        }
    }
}
