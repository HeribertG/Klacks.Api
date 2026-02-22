// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Services.Groups.Integrity;

namespace Klacks.Api.Infrastructure.Services.Groups;

public class GroupIntegrityService : IGroupIntegrityService
{
    private readonly NestedSetRepairService _repairService;
    private readonly NestedSetValidationService _validationService;
    private readonly GroupIssueFindingService _issueFindingService;
    private readonly RootIntegrityService _rootIntegrityService;
    private readonly ILogger<GroupIntegrityService> _logger;

    public GroupIntegrityService(
        NestedSetRepairService repairService,
        NestedSetValidationService validationService,
        GroupIssueFindingService issueFindingService,
        RootIntegrityService rootIntegrityService,
        ILogger<GroupIntegrityService> logger)
    {
        _repairService = repairService;
        _validationService = validationService;
        _issueFindingService = issueFindingService;
        _rootIntegrityService = rootIntegrityService;
        _logger = logger;
    }

    public async Task RepairNestedSetValuesAsync(Guid? rootId = null)
    {
        _logger.LogInformation("GroupIntegrityService: Delegating RepairNestedSetValuesAsync");
        await _repairService.RepairNestedSetValuesAsync(rootId);
    }

    public async Task FixRootValuesAsync()
    {
        _logger.LogInformation("GroupIntegrityService: Delegating FixRootValuesAsync");
        await _rootIntegrityService.FixRootValuesAsync();
    }

    public async Task<bool> ValidateNestedSetIntegrityAsync(Guid rootId)
    {
        _logger.LogInformation("GroupIntegrityService: Delegating ValidateNestedSetIntegrityAsync");
        return await _validationService.ValidateNestedSetIntegrityAsync(rootId);
    }

    public async Task RebuildSubtreeAsync(Guid rootId)
    {
        _logger.LogInformation("GroupIntegrityService: Delegating RebuildSubtreeAsync");
        await _repairService.RebuildSubtreeAsync(rootId);
    }

    public async Task<IEnumerable<Group>> FindIntegrityIssuesAsync()
    {
        _logger.LogInformation("GroupIntegrityService: Delegating FindIntegrityIssuesAsync");
        return await _issueFindingService.FindIntegrityIssuesAsync();
    }

    public IEnumerable<string> ValidateGroupData(Group group)
    {
        _logger.LogDebug("GroupIntegrityService: Delegating ValidateGroupData");
        return _validationService.ValidateGroupData(group);
    }

    public async Task EnsureReferentialIntegrityAsync()
    {
        _logger.LogInformation("GroupIntegrityService: Delegating EnsureReferentialIntegrityAsync");
        await _rootIntegrityService.EnsureReferentialIntegrityAsync();
    }

    public async Task<IEnumerable<Group>> FindOrphanedGroupsAsync()
    {
        _logger.LogInformation("GroupIntegrityService: Delegating FindOrphanedGroupsAsync");
        return await _issueFindingService.FindOrphanedGroupsAsync();
    }

    public async Task<bool> ValidateLftRgtUniquenessAsync(Guid rootId)
    {
        _logger.LogInformation("GroupIntegrityService: Delegating ValidateLftRgtUniquenessAsync");
        return await _validationService.ValidateLftRgtUniquenessAsync(rootId);
    }

    public async Task<IEnumerable<Group>> FindCircularReferencesAsync()
    {
        _logger.LogInformation("GroupIntegrityService: Delegating FindCircularReferencesAsync");
        return await _issueFindingService.FindCircularReferencesAsync();
    }

    public async Task<bool> ValidateNestedSetBoundariesAsync(Guid rootId)
    {
        _logger.LogInformation("GroupIntegrityService: Delegating ValidateNestedSetBoundariesAsync");
        return await _validationService.ValidateNestedSetBoundariesAsync(rootId);
    }

    public async Task<GroupIntegrityReport> PerformFullIntegrityCheckAsync()
    {
        _logger.LogInformation("GroupIntegrityService: Delegating PerformFullIntegrityCheckAsync");
        return await _issueFindingService.PerformFullIntegrityCheckAsync();
    }
}
