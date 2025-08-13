using AutoMapper;
using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosType
{
    public class PutCommandHandler : IRequestHandler<PutCommand, Klacks.Api.Domain.Models.Settings.MacroType?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public PutCommandHandler(SettingsApplicationService settingsApplicationService,
                                  IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.MacroType?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var result = await _settingsApplicationService.UpdateMacroTypeAsync(request.model, cancellationToken);
            await unitOfWork.CompleteAsync();
            return result;
        }
    }
}
