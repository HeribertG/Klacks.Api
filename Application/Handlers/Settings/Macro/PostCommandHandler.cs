using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class PostCommandHandler : IRequestHandler<PostCommand, MacroResource?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public PostCommandHandler(SettingsApplicationService settingsApplicationService,
                                  IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<MacroResource?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var result = await _settingsApplicationService.CreateMacroAsync(request.model, cancellationToken);

            await unitOfWork.CompleteAsync();

            return result;
        }
    }
}
