using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class PutCommandHandler : IRequestHandler<PutCommand, Models.Settings.Settings?>
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

        public async Task<Models.Settings.Settings?> Handle(PutCommand request, CancellationToken cancellationToken)
        {
            var res = await repository.PutSetting(request.model);
            await unitOfWork.CompleteAsync();
            return res;
        }
    }
}
