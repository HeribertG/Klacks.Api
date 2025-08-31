using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, Domain.Models.Settings.Settings?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public PutCommandHandler(
        ISettingsRepository settingsRepository,
                                  IMapper mapper,
                                  IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
            _settingsRepository = settingsRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Domain.Models.Settings.Settings?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var setting = _mapper.Map<Domain.Models.Settings.Settings>(request.model);
            var res = await _settingsRepository.PutSetting(setting);
            await _unitOfWork.CompleteAsync();
            return res;
        }
    }
}
