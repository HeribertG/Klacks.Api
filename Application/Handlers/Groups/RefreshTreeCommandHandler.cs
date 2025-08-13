using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class RefreshTreeCommandHandler : IRequestHandler<RefreshTreeCommand>
    {
        private readonly GroupApplicationService _groupApplicationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RefreshTreeCommandHandler> _logger;

        public RefreshTreeCommandHandler(
            GroupApplicationService groupApplicationService,
            IUnitOfWork unitOfWork,
            ILogger<RefreshTreeCommandHandler> logger)
        {
            _groupApplicationService = groupApplicationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        
        public async Task Handle(RefreshTreeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refresh the group tree");
            
            await _groupApplicationService.RefreshTreeStructureAsync(cancellationToken);
            await _unitOfWork.CompleteAsync();
        }
    }
}
