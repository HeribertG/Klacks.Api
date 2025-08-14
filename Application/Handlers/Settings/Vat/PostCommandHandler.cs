using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Vats;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class PostCommandHandler : IRequestHandler<PostCommand, Klacks.Api.Domain.Models.Settings.Vat?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork unitOfWork;

        public PostCommandHandler(ISettingsRepository settingsRepository,
                                  IMapper mapper,
                                  IUnitOfWork unitOfWork)
        {
            _settingsRepository = settingsRepository;
            _mapper = mapper;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Vat?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var vat = _mapper.Map<Klacks.Api.Domain.Models.Settings.Vat>(request.model);
            var createdVat = _settingsRepository.AddVAT(vat);

            await unitOfWork.CompleteAsync();

            return createdVat;
        }
    }
}
