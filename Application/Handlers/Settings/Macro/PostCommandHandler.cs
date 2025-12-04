using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, MacroResource?>
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

        public async Task<MacroResource?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var macro = _mapper.Map<Klacks.Api.Domain.Models.Settings.Macro>(request.model);
            var result = _settingsRepository.AddMacro(macro);

            await _unitOfWork.CompleteAsync();

            return _mapper.Map<MacroResource>(result);
        }
    }
}
