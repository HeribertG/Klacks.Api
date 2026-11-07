// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Returns one page of the group list. Every group row stays readable; the member items of groups the caller
/// cannot see are omitted (owner decision 2026-11-07).
/// </summary>
/// <param name="groupRepository">Filters and pages the groups including their member items</param>
/// <param name="groupVisibilityGuard">Decides which groups' member items the caller may see</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedGroupResource>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly GroupMapper _groupMapper;
        private readonly IGroupVisibilityGuard _groupVisibilityGuard;
        private readonly ILogger<GetTruncatedListQueryHandler> _logger;

        public GetTruncatedListQueryHandler(
            IGroupRepository groupRepository,
            GroupMapper groupMapper,
            IGroupVisibilityGuard groupVisibilityGuard,
            ILogger<GetTruncatedListQueryHandler> logger)
        {
            _groupRepository = groupRepository;
            _groupMapper = groupMapper;
            _groupVisibilityGuard = groupVisibilityGuard;
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

                await GroupMemberReadScope.HideMembersOfHiddenGroupsAsync(
                    _groupVisibilityGuard, truncatedGroupResource.Groups, cancellationToken);

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
