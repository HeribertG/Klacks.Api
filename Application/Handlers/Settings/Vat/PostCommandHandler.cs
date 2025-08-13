using Klacks.Api.Application.Commands.Settings.Vats;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class PostCommandHandler : IRequestHandler<PostCommand, Klacks.Api.Domain.Models.Settings.Vat?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public PostCommandHandler(SettingsApplicationService settingsApplicationService,
                                  IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Vat?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var createdVat = await _settingsApplicationService.CreateVatAsync(request.model, cancellationToken);

            await unitOfWork.CompleteAsync();

            return createdVat;
        }
    }
}
