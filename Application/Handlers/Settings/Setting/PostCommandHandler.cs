using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.Settings?>
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

        public async Task<Domain.Models.Settings.Settings?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var setting = _mapper.Map<Domain.Models.Settings.Settings>(request.model);
            var res = await _settingsRepository.AddSetting(setting);
            await _unitOfWork.CompleteAsync();
            return res;
        }
    }
}
