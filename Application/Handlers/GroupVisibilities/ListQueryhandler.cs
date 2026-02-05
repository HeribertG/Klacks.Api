using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class ListQueryhandler : IRequestHandler<ListQuery<GroupVisibilityResource>, IEnumerable<GroupVisibilityResource>>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly GroupMapper _groupMapper;
    private readonly ILogger<ListQueryhandler> _logger;

    public ListQueryhandler(IGroupVisibilityRepository groupVisibilityRepository, GroupMapper groupMapper, ILogger<ListQueryhandler> logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _groupMapper = groupMapper;
        _logger = logger;
    }

    public async Task<IEnumerable<GroupVisibilityResource>> Handle(ListQuery<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching group visibilities list");
        
        try
        {
            var groupVisibilities = await _groupVisibilityRepository.List();
            var visibilitiesList = groupVisibilities.ToList();
            
            _logger.LogInformation($"Retrieved {visibilitiesList.Count} group visibilities");
            
            return _groupMapper.ToGroupVisibilityResources(visibilitiesList.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching group visibilities list");
            throw new InvalidRequestException($"Failed to retrieve group visibilities list: {ex.Message}");
        }
    }
}
