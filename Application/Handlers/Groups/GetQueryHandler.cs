using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetQueryHandler : IRequestHandler<GetQuery<GroupResource>, GroupResource>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly GroupMapper _groupMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IGroupRepository groupRepository, GroupMapper groupMapper, ILogger<GetQueryHandler> logger)
        {
            _groupRepository = groupRepository;
            _groupMapper = groupMapper;
            _logger = logger;
        }

        public async Task<GroupResource> Handle(GetQuery<GroupResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting group with ID: {Id}", request.Id);
                
                var group = await _groupRepository.Get(request.Id);
                
                if (group == null)
                {
                    throw new KeyNotFoundException($"Group with ID {request.Id} not found");
                }
                
                var result = _groupMapper.ToGroupResource(group);
                _logger.LogInformation("Successfully retrieved group with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving group with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving group with ID {request.Id}: {ex.Message}");
            }
        }
    }
}