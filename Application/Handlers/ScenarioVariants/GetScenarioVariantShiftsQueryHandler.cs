// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for reading the services (SealedOrder shifts) a variant (key) carries, ordered by name then id. A missing
/// variant and a variant of a hidden group are answered with null (404); their shifts are never loaded.
/// </summary>
/// <param name="Id">Row id of the variant</param>
/// <param name="variantRepository">Loads the variant and its services</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the variant's group</param>
/// <param name="scheduleMapper">Maps the shifts to the API resource</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ScenarioVariants;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScenarioVariants;

public class GetScenarioVariantShiftsQueryHandler : BaseHandler, IRequestHandler<GetScenarioVariantShiftsQuery, List<ShiftResource>?>
{
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetScenarioVariantShiftsQueryHandler(
        IScenarioVariantRepository variantRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetScenarioVariantShiftsQueryHandler> logger)
        : base(logger)
    {
        _variantRepository = variantRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<List<ShiftResource>?> Handle(GetScenarioVariantShiftsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var variant = await ScenarioVariantAccess.FindVisibleAsync(
                _variantRepository, _groupVisibilityGuard, request.Id, cancellationToken);

            if (variant is null)
            {
                return null;
            }

            var shifts = await _variantRepository.GetSealedShiftsAsync(variant.Token, cancellationToken);

            return shifts.Select(_scheduleMapper.ToShiftResource).ToList();
        }, nameof(Handle), new { request.Id });
    }
}
