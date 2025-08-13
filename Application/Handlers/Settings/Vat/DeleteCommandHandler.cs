using Klacks.Api.Application.Commands.Settings.Vats;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class DeleteCommandHandler : IRequestHandler<DeleteCommand, Klacks.Api.Domain.Models.Settings.Vat>
    {
        private readonly SettingsApplicationService _settingsApplicationService;
        private readonly IUnitOfWork unitOfWork;

        public DeleteCommandHandler(SettingsApplicationService settingsApplicationService,
                                    IUnitOfWork unitOfWork)
        {
            _settingsApplicationService = settingsApplicationService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Vat> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var deletedVat = await _settingsApplicationService.DeleteVatAsync(request.Id, cancellationToken);

            await unitOfWork.CompleteAsync();

            return deletedVat!;
        }
    }
}
