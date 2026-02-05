using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.AssignedGroups;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.AssignedGroups
{
    public class GetListQueryHandler : IRequestHandler<AssignedGroupListQuery, IEnumerable<GroupResource>>
    {
        private readonly IAssignedGroupRepository _assignedGroupRepository;
        private readonly GroupMapper _groupMapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(IAssignedGroupRepository assignedGroupRepository, GroupMapper groupMapper, ILogger<GetListQueryHandler> logger)
        {
            _assignedGroupRepository = assignedGroupRepository;
            _groupMapper = groupMapper;
            _logger = logger;
        }

        public async Task<IEnumerable<GroupResource>> Handle(AssignedGroupListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Fetching assigned groups for ID: {request.Id}");
                
                var groups = await _assignedGroupRepository.Assigned(request.Id);
                var groupsList = groups.ToList();
                
                _logger.LogInformation($"Retrieved {groupsList.Count} assigned groups for ID: {request.Id}");
                
                return _groupMapper.ToGroupResources(groupsList.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while fetching assigned groups for ID: {request.Id}");
                throw new InvalidRequestException($"Failed to retrieve assigned groups: {ex.Message}");
            }
        }
    }
}
