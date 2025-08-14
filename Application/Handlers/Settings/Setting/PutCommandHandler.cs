using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PutCommandHandler : IRequestHandler<PutCommand, Klacks.Api.Domain.Models.Settings.Settings?>
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

        public async Task<Klacks.Api.Domain.Models.Settings.Settings?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var setting = _mapper.Map<Klacks.Api.Domain.Models.Settings.Settings>(request.model);
            var res = await _settingsRepository.PutSetting(setting);
            await unitOfWork.CompleteAsync();
            return res;
        }
    }
}
