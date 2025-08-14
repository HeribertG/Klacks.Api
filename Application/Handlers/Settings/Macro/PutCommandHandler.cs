using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class PutCommandHandler : IRequestHandler<PutCommand, MacroResource?>
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

        public async Task<MacroResource?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var macro = _mapper.Map<Klacks.Api.Domain.Models.Settings.Macro>(request.model);
            var updatedMacro = _settingsRepository.PutMacro(macro);
            await unitOfWork.CompleteAsync();
            return _mapper.Map<MacroResource>(updatedMacro);
        }
    }
}
