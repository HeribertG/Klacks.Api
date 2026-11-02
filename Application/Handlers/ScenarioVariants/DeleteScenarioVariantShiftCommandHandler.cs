// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for deleting one shift of a variant (key). A variant that is missing, hidden by group visibility or no longer
/// Active, and a shift that is missing or does not carry the token of that variant, are all answered with false (404);
/// nothing is loaded or deleted then. Otherwise the delete goes through the shift DeleteCommand, so the family delete,
/// its conflict for a family a scenario cloned and the visibility check are the ones of the shift handler.
/// </summary>
/// <param name="command">Contains the variant row id and the id of a row of the shift family</param>
/// <param name="variantRepository">Loads the tracked variant</param>
/// <param name="groupVisibilityGuard">Answers a variant outside the caller's group visibility like a missing one</param>
/// <param name="shiftRepository">Reads the stored token of the shift</param>
/// <param name="mediator">Dispatches the shift DeleteCommand</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Commands.ScenarioVariants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScenarioVariants;

public class DeleteScenarioVariantShiftCommandHandler : BaseHandler, IRequestHandler<DeleteScenarioVariantShiftCommand, bool>
{
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;

    public DeleteScenarioVariantShiftCommandHandler(
        IScenarioVariantRepository variantRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IShiftRepository shiftRepository,
        IMediator mediator,
        ILogger<DeleteScenarioVariantShiftCommandHandler> logger)
        : base(logger)
    {
        _variantRepository = variantRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _shiftRepository = shiftRepository;
        _mediator = mediator;
    }

    public async Task<bool> Handle(DeleteScenarioVariantShiftCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var variant = await ScenarioVariantAccess.FindVisibleAsync(
                _variantRepository, _groupVisibilityGuard, command.VariantId, cancellationToken);

            if (variant is null || variant.Status != AnalyseScenarioStatus.Active)
            {
                return false;
            }

            var stored = await _shiftRepository.GetNoTracking(command.ShiftId);
            if (stored?.AnalyseToken != variant.Token)
            {
                return false;
            }

            var deleted = await _mediator.Send(new DeleteCommand<ShiftResource>(command.ShiftId), cancellationToken);

            return deleted != null;
        }, nameof(Handle), new { command.VariantId, command.ShiftId });
    }
}
