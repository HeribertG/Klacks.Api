using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Handlers.Groups
{
    public class RefreshTreeCommandHandler : IRequestHandler<RefreshTreeCommand>
    {
        private readonly ILogger<RefreshTreeCommand> logger;
        private readonly IGroupRepository repository;
        private readonly IUnitOfWork unitOfWork;

        public RefreshTreeCommandHandler(IGroupRepository repository,
                                         IUnitOfWork unitOfWork,
                                         ILogger<RefreshTreeCommand> logger)
        {
            this.repository = repository;
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }
        public async Task Handle(RefreshTreeCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Refresh the group tree");
            await repository.RepairNestedSetValues();
            await unitOfWork.CompleteAsync();
            await repository.FixRootValues();
            await unitOfWork.CompleteAsync();
        }
    }
}
