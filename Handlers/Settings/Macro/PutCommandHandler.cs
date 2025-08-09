using AutoMapper;
using Klacks.Api.Commands.Settings.Macros;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Handlers.Settings.Macro
{
    public class PutCommandHandler : IRequestHandler<PutCommand, MacroResource?>
    {
        private readonly IMapper mapper;
        private readonly ISettingsRepository repository;
        private readonly IUnitOfWork unitOfWork;

        public PutCommandHandler(IMapper mapper,
                                  ISettingsRepository repository,
                                  IUnitOfWork unitOfWork)
        {
            this.mapper = mapper;
            this.repository = repository;
            this.unitOfWork = unitOfWork;
        }

        public async Task<MacroResource?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var dbMacro = await repository.GetMacro(request.model.Id);
            var updatedMacro = mapper.Map(request.model, dbMacro);

            if (updatedMacro != null)
            {
                updatedMacro = repository.PutMacro(updatedMacro);
                await unitOfWork.CompleteAsync();
                return mapper.Map<Models.Settings.Macro, MacroResource>(updatedMacro);
            }

            return null;
        }
    }
}
