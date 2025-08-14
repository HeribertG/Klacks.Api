using Klacks.Api.Application.Commands.Settings.Vats;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class DeleteCommandHandler : IRequestHandler<DeleteCommand, Klacks.Api.Domain.Models.Settings.Vat>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IUnitOfWork unitOfWork;

        public DeleteCommandHandler(ISettingsRepository settingsRepository,
                                    IUnitOfWork unitOfWork)
        {
            _settingsRepository = settingsRepository;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Vat> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var deletedVat = await _settingsRepository.DeleteVAT(request.Id);

            await unitOfWork.CompleteAsync();

            return deletedVat;
        }
    }
}
