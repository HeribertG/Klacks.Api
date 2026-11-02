// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for changing name, description and period of a variant (key). A variant of a hidden group is answered like
/// a missing one; a consumed variant (no longer Active) and one an Active scenario carries are answered with a conflict,
/// and so is a new period that would leave a shift of the variant outside it. The group cannot be changed.
/// </summary>
/// <param name="Id">Row id of the variant to change</param>
/// <param name="Request">Contains the new name, description and time period</param>
/// <param name="variantRepository">Loads the tracked variant and counts its services</param>
/// <param name="groupVisibilityGuard">Answers a variant outside the caller's group visibility like a missing one</param>
/// <param name="companyClock">Gives today in the company's time zone for the expiry flag</param>

using Klacks.Api.Application.Commands.ScenarioVariants;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScenarioVariants;

public class UpdateScenarioVariantCommandHandler : BaseHandler, IRequestHandler<UpdateScenarioVariantCommand, ScenarioVariantResource>
{
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly ICompanyClock _companyClock;

    public UpdateScenarioVariantCommandHandler(
        IScenarioVariantRepository variantRepository,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        ICompanyClock companyClock,
        ILogger<UpdateScenarioVariantCommandHandler> logger)
        : base(logger)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
        _companyClock = companyClock;
    }

    public async Task<ScenarioVariantResource> Handle(UpdateScenarioVariantCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var request = command.Request;

            ScenarioVariantValidation.EnsureDetails(request.Name, request.FromDate, request.UntilDate);

            var variant = await ScenarioVariantAccess.LoadEditableAsync(
                _variantRepository, _groupVisibilityGuard, command.Id, cancellationToken);

            var periodChanges = variant.FromDate != request.FromDate || variant.UntilDate != request.UntilDate;
            if (periodChanges && await _variantRepository.HasShiftsOutsidePeriodAsync(
                    variant.Token, request.FromDate, request.UntilDate, cancellationToken))
            {
                throw new ScenarioVariantConflictException(
                    ScenarioVariantValidation.ShiftsOutsidePeriodMessage, ScenarioVariantErrorCodes.ShiftsOutsidePeriod);
            }

            variant.Name = request.Name;
            variant.Description = request.Description;
            variant.FromDate = request.FromDate;
            variant.UntilDate = request.UntilDate;

            await _unitOfWork.CompleteAsync();

            var counts = await _variantRepository.CountShiftsByVariantTokenAsync(new[] { variant.Token }, cancellationToken);
            var today = await _companyClock.GetTodayDateAsync(cancellationToken);

            return ScenarioVariantResourceFactory.Create(variant, counts.GetValueOrDefault(variant.Token), today);
        }, nameof(Handle), new { command.Id, command.Request.Name });
    }
}
