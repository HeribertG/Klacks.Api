using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PutCommandHandler : IRequestHandler<PutCommand, Klacks.Api.Domain.Models.Settings.Settings?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public PutCommandHandler(SettingsApplicationService settingsApplicationService,
                                  IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Settings?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var res = await _settingsApplicationService.UpdateSettingAsync(request.model, cancellationToken);
            await unitOfWork.CompleteAsync();
            return res;
        }
    }
}
