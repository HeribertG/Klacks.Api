using AutoMapper;
using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosType
{
    public class PutCommandHandler : IRequestHandler<PutCommand, Klacks.Api.Domain.Models.Settings.MacroType?>
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

        public async Task<Klacks.Api.Domain.Models.Settings.MacroType?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var dbMacrosType = await repository.GetMacroType(request.model.Id);
            var updatedMacrostype = mapper.Map(request.model, dbMacrosType);

            if (updatedMacrostype != null)
            {
                updatedMacrostype = repository.PutMacroType(updatedMacrostype);
                await unitOfWork.CompleteAsync();
                return updatedMacrostype;
            }

            return null;
        }
    }
}
