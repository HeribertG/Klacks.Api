using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.AssignedGroups
{
    public class PutCommandHandler : IRequestHandler<PutCommand<AssignedGroupResource>, AssignedGroupResource?>
    {
        private readonly IAssignedGroupRepository _assignedGroupRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PutCommandHandler> _logger;

        public PutCommandHandler(
            IAssignedGroupRepository assignedGroupRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<PutCommandHandler> logger)
        {
            _assignedGroupRepository = assignedGroupRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AssignedGroupResource?> Handle(PutCommand<AssignedGroupResource> request, CancellationToken cancellationToken)
        {
            try
            {
                var existingAssignedGroup = await _assignedGroupRepository.Get(request.Resource.Id);
                if (existingAssignedGroup == null)
                {
                    _logger.LogWarning("AssignedGroup with ID {AssignedGroupId} not found.", request.Resource.Id);
                    return null;
                }

                _mapper.Map(request.Resource, existingAssignedGroup);
                await _assignedGroupRepository.Put(existingAssignedGroup);
                await _unitOfWork.CompleteAsync();
                return _mapper.Map<AssignedGroupResource>(existingAssignedGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating AssignedGroup with ID {AssignedGroupId}.", request.Resource.Id);
                throw;
            }
        }
    }
}
