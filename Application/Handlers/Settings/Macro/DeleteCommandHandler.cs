using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class DeleteCommandHandler : IRequestHandler<DeleteCommand, MacroResource>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public DeleteCommandHandler(SettingsApplicationService settingsApplicationService,
                                    IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        async Task<MacroResource> IRequestHandler<DeleteCommand, MacroResource>.Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var deletedMacro = await _settingsApplicationService.DeleteMacroAsync(request.Id, cancellationToken);

            await unitOfWork.CompleteAsync();

            return deletedMacro;
        }
    }
}
