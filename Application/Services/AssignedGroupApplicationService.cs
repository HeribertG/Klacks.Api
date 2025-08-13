using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Staffs;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class AssignedGroupApplicationService
{
    private readonly IAssignedGroupRepository _assignedGroupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AssignedGroupApplicationService> _logger;

    public AssignedGroupApplicationService(
        IAssignedGroupRepository assignedGroupRepository,
        IMapper mapper,
        ILogger<AssignedGroupApplicationService> logger)
    {
        _assignedGroupRepository = assignedGroupRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AssignedGroupResource?> GetAssignedGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var assignedGroup = await _assignedGroupRepository.Get(id);
        return assignedGroup != null ? _mapper.Map<AssignedGroupResource>(assignedGroup) : null;
    }

    public async Task<List<AssignedGroupResource>> GetAllAssignedGroupsAsync(CancellationToken cancellationToken = default)
    {
        var assignedGroups = await _assignedGroupRepository.List();
        return _mapper.Map<List<AssignedGroupResource>>(assignedGroups);
    }

    public async Task<List<GroupResource>> GetAssignedGroupsAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        var groups = await _assignedGroupRepository.Assigned(id);
        return _mapper.Map<List<GroupResource>>(groups);
    }

    public async Task<AssignedGroupResource> CreateAssignedGroupAsync(AssignedGroupResource assignedGroupResource, CancellationToken cancellationToken = default)
    {
        var assignedGroup = _mapper.Map<AssignedGroup>(assignedGroupResource);
        await _assignedGroupRepository.Add(assignedGroup);
        return _mapper.Map<AssignedGroupResource>(assignedGroup);
    }

    public async Task<AssignedGroupResource> UpdateAssignedGroupAsync(AssignedGroupResource assignedGroupResource, CancellationToken cancellationToken = default)
    {
        var assignedGroup = _mapper.Map<AssignedGroup>(assignedGroupResource);
        var updatedAssignedGroup = await _assignedGroupRepository.Put(assignedGroup);
        return _mapper.Map<AssignedGroupResource>(updatedAssignedGroup);
    }

    public async Task DeleteAssignedGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _assignedGroupRepository.Delete(id);
    }
}