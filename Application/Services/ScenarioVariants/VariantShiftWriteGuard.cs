// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Implements the write rules for shifts in a variant (key). A hidden variant is answered like a missing shift (404, same
/// text, the id of the variant row stays out of it), a consumed one and one an Active scenario carries with the conflicts of
/// ScenarioVariantAccess (409), every content problem with a BadRequestException (400)
/// like the shift handlers that call this guard. A shift in a variant needs at least one group, because a group-less
/// shift is visible to everyone (ShiftGroupVisibilityFilter), and a customer, although a sealed order may otherwise have
/// none (ClientlessOrderRules), and it lies with start and end inside the variant's period, so no key shift can become
/// retroactive or open-ended demand when it is accepted. An edit of the sealed order or its plannable copy that changes a cut-relevant field
/// (VariantShiftMirror) is a conflict while the order has SplitShift rows, the same edit of a SplitShift row always is
/// (its fields belong to the cut); the repository mirrors the rest. Cutting and
/// resetting cuts in a variant needs only the write rule (EnsureOpenIfVariantAsync); reading a family needs less
/// (IsReadableVariantAsync), a key in use stays readable and a hidden one looks like a missing id. Apart from the period
/// (EnsureInsideVariantPeriodAsync, which ShiftCutFacade calls for the dates of a cut and of a reset) this guard does not read
/// the content of a cut: the other content rules hold because ShiftCutFacade takes the customer and the groups of a new piece
/// from its parent, and ShiftRepository.PutCutUpdate keeps the stored groups of an updated piece, whatever the request carries.
/// </summary>
/// <param name="scenarioRepository">Resolves the row behind a token, of either kind</param>
/// <param name="variantRepository">Reads the shift family of a variant and its scenario clones</param>
/// <param name="scenarioService">Resolves the variant group with its subgroups</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the variant's group</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Application.Services.ScenarioVariants;

public class VariantShiftWriteGuard : IVariantShiftWriteGuard
{
    public const string NotAVariantMessage = "A shift can only be created with the token of a variant";

    public const string CustomerRequiredMessage = "A shift in a variant needs a customer";

    public const string GroupRequiredMessage = "A shift in a variant needs a group";

    public const string GroupOutsideVariantMessage = "Every group of a shift in a variant must lie in the variant's group or its subgroups";

    public const string StatusIsServerOwnedMessage = "The status of a shift in a variant cannot be changed";

    public const string ClonedByScenarioMessage = "The shift is still carried by a scenario and cannot be deleted from the variant";

    public const string PeriodOutsideVariantMessage = "A shift in a variant must lie inside the variant's period";

    public const string CutsExistMessage = "The shift has cuts; reset the cuts before changing its date, time, weekdays, staffing, customer or type";

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public VariantShiftWriteGuard(
        IAnalyseScenarioRepository scenarioRepository,
        IScenarioVariantRepository variantRepository,
        IAnalyseScenarioService scenarioService,
        IGroupVisibilityGuard groupVisibilityGuard)
    {
        _scenarioRepository = scenarioRepository;
        _variantRepository = variantRepository;
        _scenarioService = scenarioService;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task EnsureCreatableAsync(Guid variantToken, Shift shift, CancellationToken cancellationToken = default)
    {
        var variant = await FindVariantAsync(variantToken, cancellationToken)
            ?? throw new BadRequestException(NotAVariantMessage);

        await EnsureOpenAsync(variant, shift.Id, cancellationToken);
        await EnsureContentAsync(variant, shift, cancellationToken);
    }

    public async Task EnsureEditableAsync(Shift stored, Shift incoming, CancellationToken cancellationToken = default)
    {
        if (stored.AnalyseToken is not { } token
            || await FindVariantAsync(token, cancellationToken) is not { } variant)
        {
            return;
        }

        await EnsureOpenAsync(variant, stored.Id, cancellationToken);

        if (stored.Status != incoming.Status)
        {
            throw new BadRequestException(StatusIsServerOwnedMessage);
        }

        await EnsureContentAsync(variant, incoming, cancellationToken);
        await EnsureNoCutConflictAsync(token, stored, incoming, cancellationToken);
    }

    public async Task<bool> EnsureOpenIfVariantAsync(Guid token, Guid shiftId, CancellationToken cancellationToken = default)
    {
        if (await FindVariantAsync(token, cancellationToken) is not { } variant)
        {
            return false;
        }

        await EnsureOpenAsync(variant, shiftId, cancellationToken);
        return true;
    }

    public async Task EnsureInsideVariantPeriodAsync(
        Guid token, IReadOnlyCollection<(DateOnly From, DateOnly? Until)> ranges, CancellationToken cancellationToken = default)
    {
        if (await FindVariantAsync(token, cancellationToken) is not { } variant)
        {
            return;
        }

        foreach (var (from, until) in ranges)
        {
            EnsureInsidePeriod(variant, from, until);
        }
    }

    public async Task<bool> IsReadableVariantAsync(Guid token, CancellationToken cancellationToken = default)
    {
        if (await FindVariantAsync(token, cancellationToken) is not { } variant
            || !await _groupVisibilityGuard.IsScenarioVisibleAsync(variant, cancellationToken))
        {
            return false;
        }

        ScenarioVariantAccess.EnsureNotConsumed(variant);
        return true;
    }

    public async Task<List<Guid>?> ResolveDeletableFamilyAsync(Shift stored, CancellationToken cancellationToken = default)
    {
        if (stored.AnalyseToken is not { } token
            || await FindVariantAsync(token, cancellationToken) is not { } variant)
        {
            return null;
        }

        await EnsureOpenAsync(variant, stored.Id, cancellationToken);

        var family = await _variantRepository.GetShiftFamilyIdsAsync(token, VariantShiftFamilyRules.FamilyRootOrSelf(stored), cancellationToken);
        if (await _variantRepository.HasScenarioClonesAsync(family, cancellationToken))
        {
            throw new ScenarioVariantConflictException(ClonedByScenarioMessage, ScenarioVariantErrorCodes.ShiftCloned);
        }

        return family;
    }

    private async Task<AnalyseScenario?> FindVariantAsync(Guid token, CancellationToken cancellationToken)
    {
        var row = await _scenarioRepository.GetByTokenAsync(token, cancellationToken);
        return row?.Kind == AnalyseScenarioKind.Variant ? row : null;
    }

    private async Task EnsureOpenAsync(AnalyseScenario variant, Guid shiftId, CancellationToken cancellationToken)
    {
        if (!await _groupVisibilityGuard.IsScenarioVisibleAsync(variant, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, shiftId));
        }

        await ScenarioVariantAccess.EnsureWritableAsync(_variantRepository, variant, cancellationToken);
    }

    private async Task EnsureNoCutConflictAsync(Guid token, Shift stored, Shift incoming, CancellationToken cancellationToken)
    {
        if (VariantShiftMirror.FindCutRelevantChanges(stored, incoming).Count == 0)
        {
            return;
        }

        var isCutPiece = stored.Status == ShiftStatus.SplitShift;
        var isFamilyRow = stored.Status is ShiftStatus.SealedOrder or ShiftStatus.OriginalShift;
        if (isCutPiece
            || (isFamilyRow && await _variantRepository.HasCutsAsync(token, VariantShiftFamilyRules.FamilyRootOrSelf(stored), cancellationToken)))
        {
            throw new ScenarioVariantConflictException(CutsExistMessage, ScenarioVariantErrorCodes.ShiftHasCuts);
        }
    }

    private async Task EnsureContentAsync(AnalyseScenario variant, Shift shift, CancellationToken cancellationToken)
    {
        if (!shift.ClientId.HasValue)
        {
            throw new BadRequestException(CustomerRequiredMessage);
        }

        EnsureInsidePeriod(variant, shift.FromDate, shift.UntilDate);
        await EnsureGroupsInsideVariantAsync(variant, shift.GroupItems, cancellationToken);
    }

    private static void EnsureInsidePeriod(AnalyseScenario variant, DateOnly from, DateOnly? until)
    {
        if (!VariantShiftFamilyRules.LiesInsidePeriod(from, until, variant.FromDate, variant.UntilDate))
        {
            throw new BadRequestException(PeriodOutsideVariantMessage);
        }
    }

    private async Task EnsureGroupsInsideVariantAsync(
        AnalyseScenario variant, IEnumerable<GroupItem> groupItems, CancellationToken cancellationToken)
    {
        var groupIds = groupItems.Where(item => !item.IsDeleted).Select(item => item.GroupId).Distinct().ToList();
        if (groupIds.Count == 0)
        {
            throw new BadRequestException(GroupRequiredMessage);
        }

        if (variant.GroupId is not { } variantGroupId)
        {
            throw new BadRequestException(GroupOutsideVariantMessage);
        }

        var allowed = await _scenarioService.GetGroupHierarchyIdsAsync(variantGroupId, cancellationToken);
        if (!groupIds.All(allowed.Contains))
        {
            throw new BadRequestException(GroupOutsideVariantMessage);
        }
    }
}
