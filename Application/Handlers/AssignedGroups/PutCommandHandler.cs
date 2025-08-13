using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.AssignedGroups
{
    public class PutCommandHandler : IRequestHandler<PutCommand<AssignedGroupResource>, AssignedGroupResource?>
    {
        private readonly AssignedGroupApplicationService _assignedGroupApplicationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PutCommandHandler> _logger;

        public PutCommandHandler(
            AssignedGroupApplicationService assignedGroupApplicationService,
            IUnitOfWork unitOfWork,
            ILogger<PutCommandHandler> logger)
        {
            _assignedGroupApplicationService = assignedGroupApplicationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AssignedGroupResource?> Handle(PutCommand<AssignedGroupResource> request, CancellationToken cancellationToken)
        {
            try
            {
                var existingAssignedGroup = await _assignedGroupApplicationService.GetAssignedGroupByIdAsync(request.Resource.Id, cancellationToken);
                if (existingAssignedGroup == null)
                {
                    _logger.LogWarning("AssignedGroup with ID {AssignedGroupId} not found.", request.Resource.Id);
                    return null;
                }

                var result = await _assignedGroupApplicationService.UpdateAssignedGroupAsync(request.Resource, cancellationToken);
                await _unitOfWork.CompleteAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating AssignedGroup with ID {AssignedGroupId}.", request.Resource.Id);
                throw;
            }
        }
    }
}
