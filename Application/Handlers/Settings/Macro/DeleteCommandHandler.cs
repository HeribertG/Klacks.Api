using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand, MacroResource>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCommandHandler(
        ISettingsRepository settingsRepository,
                                    SettingsMapper settingsMapper,
                                    IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
            _settingsRepository = settingsRepository;
            _settingsMapper = settingsMapper;
            _unitOfWork = unitOfWork;
        }

        async Task<MacroResource> IRequestHandler<DeleteCommand, MacroResource>.Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var deletedMacro = await _settingsRepository.DeleteMacro(request.Id);

            await _unitOfWork.CompleteAsync();

            return _settingsMapper.ToMacroResource(deletedMacro);
        }
    }
}
