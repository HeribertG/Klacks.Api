using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class RefreshTreeCommandHandler : BaseHandler, IRequestHandler<RefreshTreeCommand>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IUnitOfWork _unitOfWork;
        
        public RefreshTreeCommandHandler(
            IGroupRepository groupRepository,
            IUnitOfWork unitOfWork,
            ILogger<RefreshTreeCommandHandler> logger)
        : base(logger)
        {
            _groupRepository = groupRepository;
            _unitOfWork = unitOfWork;
            }
        
        public async Task Handle(RefreshTreeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refresh the group tree");
            
            await _groupRepository.RepairNestedSetValues();
            await _unitOfWork.CompleteAsync();
        }
    }
}
