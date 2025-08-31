using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Vats;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.Vat?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public PostCommandHandler(
        ISettingsRepository settingsRepository,
                                  IMapper mapper,
                                  IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
            _settingsRepository = settingsRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.Vat?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var vat = _mapper.Map<Domain.Models.Settings.Vat>(request.model);
            var createdVat = _settingsRepository.AddVAT(vat);

            await _unitOfWork.CompleteAsync();

            return createdVat;
        }
    }
}
