using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetQueryHandler : IRequestHandler<GetQuery<GroupResource>, GroupResource>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IGroupRepository groupRepository, IMapper mapper, ILogger<GetQueryHandler> logger)
        {
            _groupRepository = groupRepository;
            _mapper = mapper;
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
                
                var result = _mapper.Map<GroupResource>(group);
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