using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Services.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, Domain.Models.Settings.Settings?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISettingsEncryptionService _encryptionService;
        private readonly IUnitOfWork _unitOfWork;

        public PutCommandHandler(
            ISettingsRepository settingsRepository,
            ISettingsEncryptionService encryptionService,
            IUnitOfWork unitOfWork,
            ILogger<PutCommandHandler> logger)
            : base(logger)
        {
            _settingsRepository = settingsRepository;
            _encryptionService = encryptionService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.Settings?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            request.model.Value = _encryptionService.ProcessForStorage(request.model.Type, request.model.Value);
            var res = await _settingsRepository.PutSetting(request.model);
            await _unitOfWork.CompleteAsync();
            return res;
        }
    }
}
