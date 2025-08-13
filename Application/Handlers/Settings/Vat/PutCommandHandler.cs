using Klacks.Api.Application.Commands.Settings.Vats;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class PutCommandHandler : IRequestHandler<PutCommand, Klacks.Api.Domain.Models.Settings.Vat?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public PutCommandHandler(SettingsApplicationService settingsApplicationService,
                                  IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Vat?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var updatedVat = await _settingsApplicationService.UpdateVatAsync(request.model, cancellationToken);
            await unitOfWork.CompleteAsync();
            return updatedVat;
        }
    }
}
