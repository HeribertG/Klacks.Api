// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps a recipe ask-step slot name to the real, non-deleted entity names it refers to.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SuggestionEntityNameReader : ISuggestionEntityNameReader
{
    private const string ContractSlot = "contractName";
    private const string GroupSlot = "groupName";

    private readonly IContractRepository _contractRepository;
    private readonly IGroupRepository _groupRepository;

    public SuggestionEntityNameReader(IContractRepository contractRepository, IGroupRepository groupRepository)
    {
        _contractRepository = contractRepository;
        _groupRepository = groupRepository;
    }

    public async Task<IReadOnlyList<string>?> GetRealNamesForSlotAsync(string slotName, CancellationToken cancellationToken = default)
    {
        switch (slotName)
        {
            case ContractSlot:
                var contracts = await _contractRepository.List();
                return contracts.Where(c => !c.IsDeleted).Select(c => c.Name).ToList();
            case GroupSlot:
                var groups = await _groupRepository.List();
                return groups.Where(g => !g.IsDeleted).Select(g => g.Name).ToList();
            default:
                return null;
        }
    }
}
