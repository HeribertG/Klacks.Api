﻿using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class GetQueryHandler : IRequestHandler<GetQuery<GroupVisibilityResource>, GroupVisibilityResource>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(IGroupVisibilityRepository groupVisibilityRepository, IMapper mapper, ILogger<GetQueryHandler> logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GroupVisibilityResource> Handle(GetQuery<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting group visibility with ID: {Id}", request.Id);
            
            var groupVisibility = await _groupVisibilityRepository.Get(request.Id);
            
            if (groupVisibility == null)
            {
                throw new KeyNotFoundException($"Group visibility with ID {request.Id} not found");
            }
            
            var result = _mapper.Map<GroupVisibilityResource>(groupVisibility);
            _logger.LogInformation("Successfully retrieved group visibility with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group visibility with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving group visibility with ID {request.Id}: {ex.Message}");
        }
    }
}
