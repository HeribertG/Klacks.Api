using AutoMapper;
using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosType
{
    public class PostCommandHandler : IRequestHandler<PostCommand, Models.Settings.MacroType?>
    {
        private readonly IMapper mapper;
        private readonly ISettingsRepository repository;
        private readonly IUnitOfWork unitOfWork;

        public PostCommandHandler(IMapper mapper,
                                  ISettingsRepository repository,
                                  IUnitOfWork unitOfWork)
        {
            this.mapper = mapper;
            this.repository = repository;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Models.Settings.MacroType?> Handle(PostCommand request, CancellationToken cancellationToken)
        {
            repository.AddMacroType(request.model);

            await unitOfWork.CompleteAsync();

            return request.model;
        }
    }
}
