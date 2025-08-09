using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class DeleteCommandHandler : IRequestHandler<DeleteCommand, MacroResource>
    {
        private readonly IMapper mapper;
        private readonly ISettingsRepository repository;
        private readonly IUnitOfWork unitOfWork;

        public DeleteCommandHandler(IMapper mapper,
                                    ISettingsRepository repository,
                                    IUnitOfWork unitOfWork)
        {
            this.mapper = mapper;
            this.repository = repository;
            this.unitOfWork = unitOfWork;
        }

        async Task<MacroResource> IRequestHandler<DeleteCommand, MacroResource>.Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var client = await repository.DeleteMacro(request.Id);

            await unitOfWork.CompleteAsync();

            return mapper.Map<Models.Settings.Macro, MacroResource>(client!);
        }
    }
}
