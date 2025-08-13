using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class PutCommandHandler : IRequestHandler<PutCommand, MacroResource?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public PutCommandHandler(SettingsApplicationService settingsApplicationService,
                                  IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<MacroResource?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var updatedMacro = await _settingsApplicationService.UpdateMacroAsync(request.model, cancellationToken);
            await unitOfWork.CompleteAsync();
            return updatedMacro;
        }
    }
}
