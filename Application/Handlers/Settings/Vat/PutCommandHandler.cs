using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Vats;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class PutCommandHandler : IRequestHandler<PutCommand, Klacks.Api.Domain.Models.Settings.Vat?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork unitOfWork;

        public PutCommandHandler(ISettingsRepository settingsRepository,
                                  IMapper mapper,
                                  IUnitOfWork unitOfWork)
        {
            _settingsRepository = settingsRepository;
            _mapper = mapper;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Vat?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var vat = _mapper.Map<Klacks.Api.Domain.Models.Settings.Vat>(request.model);
            var updatedVat = _settingsRepository.PutVAT(vat);
            await unitOfWork.CompleteAsync();
            return updatedVat;
        }
    }
}
