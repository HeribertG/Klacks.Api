using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class PostCommandHandler : IRequestHandler<PostCommand, MacroResource?>
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

        public async Task<MacroResource?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            var macro = _mapper.Map<Klacks.Api.Domain.Models.Settings.Macro>(request.model);
            var result = _settingsRepository.AddMacro(macro);

            await unitOfWork.CompleteAsync();

            return _mapper.Map<MacroResource>(result);
        }
    }
}
