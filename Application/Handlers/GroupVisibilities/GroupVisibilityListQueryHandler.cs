using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.GroupVisibilities;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class GroupVisibilityListQueryHandler : IRequestHandler<GroupVisibilityListQuery, IEnumerable<GroupVisibilityResource>>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GroupVisibilityListQueryHandler> _logger;

    public GroupVisibilityListQueryHandler(IGroupVisibilityRepository groupVisibilityRepository, IMapper mapper, ILogger<GroupVisibilityListQueryHandler> logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<GroupVisibilityResource>> Handle(GroupVisibilityListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Fetching group visibilities list for ID: {request.Id}");
        
        if (string.IsNullOrEmpty(request.Id))
        {
            _logger.LogWarning("ID parameter is null or empty for group visibilities list");
            throw new InvalidRequestException("ID parameter is required for group visibilities list");
        }
        
        try
        {
            var groupVisibilities = await _groupVisibilityRepository.GroupVisibilityList(request.Id);
            var visibilitiesList = groupVisibilities.ToList();
            
            _logger.LogInformation($"Retrieved {visibilitiesList.Count} group visibilities for ID: {request.Id}");
            
            return _mapper.Map<IEnumerable<GroupVisibilityResource>>(visibilitiesList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while fetching group visibilities for ID: {request.Id}");
            throw new InvalidRequestException($"Failed to retrieve group visibilities for ID {request.Id}: {ex.Message}");
        }
    }
}
