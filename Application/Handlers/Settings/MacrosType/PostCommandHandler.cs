using AutoMapper;
using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosType
{
    public class PostCommandHandler : IRequestHandler<PostCommand, Klacks.Api.Domain.Models.Settings.MacroType?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public PostCommandHandler(SettingsApplicationService settingsApplicationService,
                                  IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.MacroType?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var result = await _settingsApplicationService.CreateMacroTypeAsync(request.model, cancellationToken);

            await unitOfWork.CompleteAsync();

            return result;
        }
    }
}
