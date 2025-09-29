using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.AssignedGroups;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.AssignedGroups
{
    public class GetListQueryHandler : IRequestHandler<AssignedGroupListQuery, IEnumerable<GroupResource>>
    {
        private readonly IAssignedGroupRepository _assignedGroupRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(IAssignedGroupRepository assignedGroupRepository, IMapper mapper, ILogger<GetListQueryHandler> logger)
        {
            _assignedGroupRepository = assignedGroupRepository;
            _mapper = mapper;
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
                
                return _mapper.Map<IEnumerable<GroupResource>>(groupsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while fetching assigned groups for ID: {request.Id}");
                throw new InvalidRequestException($"Failed to retrieve assigned groups: {ex.Message}");
            }
        }
    }
}
