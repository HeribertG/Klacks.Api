using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.AssignedGroups;

public class GetQueryHandler : IRequestHandler<GetQuery<AssignedGroupResource>, AssignedGroupResource>
{
    private readonly IAssignedGroupRepository _assignedGroupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(IAssignedGroupRepository assignedGroupRepository, IMapper mapper, ILogger<GetQueryHandler> logger)
    {
        _assignedGroupRepository = assignedGroupRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AssignedGroupResource> Handle(GetQuery<AssignedGroupResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting assigned group with ID: {Id}", request.Id);
            
            var assignedGroup = await _assignedGroupRepository.Get(request.Id);
            
            if (assignedGroup == null)
            {
                throw new KeyNotFoundException($"Assigned group with ID {request.Id} not found");
            }
            
            var result = _mapper.Map<AssignedGroupResource>(assignedGroup);
            _logger.LogInformation("Successfully retrieved assigned group with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assigned group with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving assigned group with ID {request.Id}: {ex.Message}");
        }
    }
}
