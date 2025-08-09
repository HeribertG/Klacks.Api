using AutoMapper;
using Klacks.Api.Application.Commands.Settings.Vats;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class DeleteCommandHandler : IRequestHandler<DeleteCommand, Models.Settings.Vat>
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

        public async Task<Models.Settings.Vat> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var vat = await repository.DeleteVAT(request.Id);

            await unitOfWork.CompleteAsync();

            return vat;
        }
    }
}
