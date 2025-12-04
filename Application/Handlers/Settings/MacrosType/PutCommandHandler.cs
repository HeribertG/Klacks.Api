using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.MacrosType
{
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, Domain.Models.Settings.MacroType?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PutCommandHandler(
        ISettingsRepository settingsRepository,
                                  IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
            _settingsRepository = settingsRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.MacroType?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var result = _settingsRepository.PutMacroType(request.model);
            await _unitOfWork.CompleteAsync();
            return result;
        }
    }
}
