// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for copying existing real shifts, each with its whole order family, into a variant (key). Every refusal comes
/// before anything is staged: a request that names both or neither of shifts and source group is a bad request; a variant
/// of a hidden group is answered like a missing one, a consumed variant and one an Active scenario carries with a conflict; a source group the caller
/// cannot see is answered like a missing group and one outside the variant's group hierarchy is a bad request; a shift the
/// caller cannot see is answered like a missing shift. The copier then applies the content rules, and the handler commits
/// the staged copies in one save.
/// </summary>
/// <param name="command">Contains the variant row id and either the shift ids to copy or the source group</param>
/// <param name="variantRepository">Loads the tracked variant</param>
/// <param name="groupVisibilityGuard">Answers a variant or source group outside the caller's group visibility like a missing one</param>
/// <param name="scenarioService">Resolves the variant group with its subgroups</param>
/// <param name="shiftVisibilityGuard">Answers a shift the caller may not see like a missing one</param>
/// <param name="copier">Loads the families and stages their copies</param>
/// <param name="unitOfWork">Commits the staged copies</param>

using Klacks.Api.Application.Commands.ScenarioVariants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScenarioVariants;

public class CopyShiftsToVariantCommandHandler : BaseHandler, IRequestHandler<CopyShiftsToVariantCommand, CopyShiftsToVariantResult>
{
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IScenarioVariantShiftCopier _copier;
    private readonly IUnitOfWork _unitOfWork;

    public CopyShiftsToVariantCommandHandler(
        IScenarioVariantRepository variantRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IAnalyseScenarioService scenarioService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IScenarioVariantShiftCopier copier,
        IUnitOfWork unitOfWork,
        ILogger<CopyShiftsToVariantCommandHandler> logger)
        : base(logger)
    {
        _variantRepository = variantRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _scenarioService = scenarioService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _copier = copier;
        _unitOfWork = unitOfWork;
    }

    public async Task<CopyShiftsToVariantResult> Handle(CopyShiftsToVariantCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var request = command.Request;
            var shiftIds = ScenarioVariantValidation.EnsureCopySelection(request.ShiftIds, request.SourceGroupId);

            var variant = await ScenarioVariantAccess.LoadEditableAsync(
                _variantRepository, _groupVisibilityGuard, command.VariantId, cancellationToken);
            var variantGroupId = variant.GroupId
                ?? throw new InvalidRequestException(ScenarioVariantValidation.GroupRequiredMessage);
            var variantGroupIds = await _scenarioService.GetGroupHierarchyIdsAsync(variantGroupId, cancellationToken);

            var result = shiftIds is not null
                ? await CopyShiftsAsync(variant, variantGroupIds, shiftIds, cancellationToken)
                : await CopyGroupAsync(variant, variantGroupIds, request.SourceGroupId!.Value, cancellationToken);

            await _unitOfWork.CompleteAsync();

            return result;
        }, nameof(Handle), new { command.VariantId });
    }

    private async Task<CopyShiftsToVariantResult> CopyShiftsAsync(
        AnalyseScenario variant, List<Guid> variantGroupIds, List<Guid> shiftIds, CancellationToken cancellationToken)
    {
        if (await _shiftVisibilityGuard.FindFirstHiddenAsync(shiftIds, cancellationToken) is { } hiddenId)
        {
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, hiddenId));
        }

        return await _copier.StageCopyOfShiftsAsync(variant, variantGroupIds, shiftIds, cancellationToken);
    }

    private async Task<CopyShiftsToVariantResult> CopyGroupAsync(
        AnalyseScenario variant, List<Guid> variantGroupIds, Guid sourceGroupId, CancellationToken cancellationToken)
    {
        if (!await _groupVisibilityGuard.IsGroupVisibleAsync(sourceGroupId, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.GroupNotFound, sourceGroupId));
        }

        if (!variantGroupIds.Contains(sourceGroupId))
        {
            throw new InvalidRequestException(ScenarioVariantValidation.SourceGroupOutsideVariantMessage);
        }

        return await _copier.StageCopyOfGroupAsync(variant, variantGroupIds, sourceGroupId, cancellationToken);
    }
}
