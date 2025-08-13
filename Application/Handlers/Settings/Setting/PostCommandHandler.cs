using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PostCommandHandler : IRequestHandler<PostCommand, Klacks.Api.Domain.Models.Settings.Settings?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public PostCommandHandler(SettingsApplicationService settingsApplicationService,
                                  IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Settings?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var res = await _settingsApplicationService.CreateSettingAsync(request.model, cancellationToken);
            await unitOfWork.CompleteAsync();
            return res;
        }
    }
}
