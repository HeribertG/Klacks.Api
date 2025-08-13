using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class GroupVisibilityApplicationService
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GroupVisibilityApplicationService> _logger;

    public GroupVisibilityApplicationService(
        IGroupVisibilityRepository groupVisibilityRepository,
        IMapper mapper,
        ILogger<GroupVisibilityApplicationService> logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GroupVisibilityResource?> GetGroupVisibilityByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var groupVisibility = await _groupVisibilityRepository.Get(id);
        return groupVisibility != null ? _mapper.Map<GroupVisibilityResource>(groupVisibility) : null;
    }

    public async Task<List<GroupVisibilityResource>> GetAllGroupVisibilitiesAsync(CancellationToken cancellationToken = default)
    {
        var groupVisibilities = await _groupVisibilityRepository.List();
        return _mapper.Map<List<GroupVisibilityResource>>(groupVisibilities);
    }

    public async Task<List<GroupVisibilityResource>> GetGroupVisibilityListAsync(string id, CancellationToken cancellationToken = default)
    {
        var groupVisibilities = await _groupVisibilityRepository.GroupVisibilityList(id);
        return _mapper.Map<List<GroupVisibilityResource>>(groupVisibilities);
    }

    public async Task<List<GroupVisibilityResource>> GetGroupVisibilityListAsync(CancellationToken cancellationToken = default)
    {
        var groupVisibilities = await _groupVisibilityRepository.GetGroupVisibilityList();
        return _mapper.Map<List<GroupVisibilityResource>>(groupVisibilities);
    }

    public async Task SetGroupVisibilityListAsync(List<GroupVisibilityResource> groupVisibilityResources, CancellationToken cancellationToken = default)
    {
        var groupVisibilities = _mapper.Map<List<GroupVisibility>>(groupVisibilityResources);
        await _groupVisibilityRepository.SetGroupVisibilityList(groupVisibilities);
    }

    public async Task<GroupVisibilityResource> CreateGroupVisibilityAsync(GroupVisibilityResource groupVisibilityResource, CancellationToken cancellationToken = default)
    {
        var groupVisibility = _mapper.Map<GroupVisibility>(groupVisibilityResource);
        await _groupVisibilityRepository.Add(groupVisibility);
        return _mapper.Map<GroupVisibilityResource>(groupVisibility);
    }

    public async Task<GroupVisibilityResource> UpdateGroupVisibilityAsync(GroupVisibilityResource groupVisibilityResource, CancellationToken cancellationToken = default)
    {
        var groupVisibility = _mapper.Map<GroupVisibility>(groupVisibilityResource);
        var updatedGroupVisibility = await _groupVisibilityRepository.Put(groupVisibility);
        return _mapper.Map<GroupVisibilityResource>(updatedGroupVisibility);
    }

    public async Task DeleteGroupVisibilityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _groupVisibilityRepository.Delete(id);
    }
}