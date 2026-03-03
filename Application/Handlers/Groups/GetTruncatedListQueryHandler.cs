// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedGroupResource>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly GroupMapper _groupMapper;
        private readonly ILogger<GetTruncatedListQueryHandler> _logger;

        public GetTruncatedListQueryHandler(IGroupRepository groupRepository, GroupMapper groupMapper, ILogger<GetTruncatedListQueryHandler> logger)
        {
            _groupRepository = groupRepository;
            _groupMapper = groupMapper;
            _logger = logger;
        }

        public async Task<TruncatedGroupResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching truncated groups list");
            
            if (request.Filter == null)
            {
                _logger.LogWarning("Filter parameter is null for truncated groups list");
                throw new InvalidRequestException("Filter parameter is required for truncated groups list");
            }
            
            try
            {
                var truncatedResult = await _groupRepository.Truncated(request.Filter);
                
                var truncatedGroupResource = new TruncatedGroupResource
                {
                    Groups = _groupMapper.ToGroupResources(truncatedResult.Groups.ToList()),
                    MaxItems = truncatedResult.MaxItems,
                    MaxPages = truncatedResult.MaxPages,
                    CurrentPage = truncatedResult.CurrentPage,
                    FirstItemOnPage = truncatedResult.FirstItemOnPage
                };
                
                _logger.LogInformation("Retrieved truncated groups list with {Count} groups", truncatedGroupResource.Groups.Count);
                
                return truncatedGroupResource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching truncated groups list");
                throw new InvalidRequestException($"Failed to retrieve truncated groups list: {ex.Message}");
            }
        }
    }
}
