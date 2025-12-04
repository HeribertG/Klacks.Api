using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.MacrosType
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.MacroType?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PostCommandHandler(
        ISettingsRepository settingsRepository,
                                  IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
            _settingsRepository = settingsRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.MacroType?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var result = _settingsRepository.AddMacroType(request.model);

            await _unitOfWork.CompleteAsync();

            return result;
        }
    }
}
