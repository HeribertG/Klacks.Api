using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class RefreshTreeCommandHandler : IRequestHandler<RefreshTreeCommand>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RefreshTreeCommandHandler> _logger;

        public RefreshTreeCommandHandler(
            IGroupRepository groupRepository,
            IUnitOfWork unitOfWork,
            ILogger<RefreshTreeCommandHandler> logger)
        {
            _groupRepository = groupRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        
        public async Task Handle(RefreshTreeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refresh the group tree");
            
            await _groupRepository.RepairNestedSetValues();
            await _unitOfWork.CompleteAsync();
        }
    }
}
