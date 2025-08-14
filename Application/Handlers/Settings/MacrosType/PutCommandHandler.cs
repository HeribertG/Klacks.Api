using AutoMapper;
using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosType
{
    public class PutCommandHandler : IRequestHandler<PutCommand, Klacks.Api.Domain.Models.Settings.MacroType?>
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

        public async Task<Klacks.Api.Domain.Models.Settings.MacroType?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var macroType = _mapper.Map<Klacks.Api.Domain.Models.Settings.MacroType>(request.model);
            var result = _settingsRepository.PutMacroType(macroType);
            await unitOfWork.CompleteAsync();
            return result;
        }
    }
}
