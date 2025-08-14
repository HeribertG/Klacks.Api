using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class DeleteCommandHandler : IRequestHandler<DeleteCommand, MacroResource>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork unitOfWork;

        public DeleteCommandHandler(ISettingsRepository settingsRepository,
                                    IMapper mapper,
                                    IUnitOfWork unitOfWork)
        {
            _settingsRepository = settingsRepository;
            _mapper = mapper;
            this.unitOfWork = unitOfWork;
        }

        async Task<MacroResource> IRequestHandler<DeleteCommand, MacroResource>.Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var deletedMacro = await _settingsRepository.DeleteMacro(request.Id);

            await unitOfWork.CompleteAsync();

            return _mapper.Map<MacroResource>(deletedMacro);
        }
    }
}
