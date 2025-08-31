using Klacks.Api.Application.Commands.Settings.Vats;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand, Domain.Models.Settings.Vat>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCommandHandler(
        ISettingsRepository settingsRepository,
                                    IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
            _settingsRepository = settingsRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.Vat> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var deletedVat = await _settingsRepository.DeleteVAT(request.Id);

            await _unitOfWork.CompleteAsync();

            return deletedVat;
        }
    }
}
