// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

/// <summary>
/// Handler for loading one shift by id. A shift of a group the caller may not see is answered exactly like a
/// missing one (same KeyNotFound text), before any other check, so neither its data nor its existence leaks. So is a
/// shift of a variant (key) the caller may not see. A shift with a token is only served when that token is a variant the
/// caller may see; a scenario clone stays refused.
/// </summary>
/// <param name="shiftRepository">Loads the shift with its group items</param>
/// <param name="shiftGroupFilterService">Resolves the caller's group restriction (null = unrestricted)</param>
/// <param name="scheduleMapper">Maps the shift to the API resource</param>
/// <param name="scenarioTokenAccessGuard">Tells a visible variant token from every other token</param>
/// <param name="logger">Diagnostics logging</param>
public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ShiftResource>, ShiftResource>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;

    public GetQueryHandler(
        IShiftRepository shiftRepository,
        IShiftGroupFilterService shiftGroupFilterService,
        ScheduleMapper scheduleMapper,
        IScenarioTokenAccessGuard scenarioTokenAccessGuard,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _shiftGroupFilterService = shiftGroupFilterService;
        _scheduleMapper = scheduleMapper;
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
    }

    public async Task<ShiftResource> Handle(GetQuery<ShiftResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var shift = await _shiftRepository.Get(request.Id);

            if (shift == null
                || !ShiftGroupVisibilityFilter.IsVisible(shift, await _shiftGroupFilterService.ResolveShiftListGroupIdsAsync(null))
                || (shift.AnalyseToken is { } storedToken
                    && await _scenarioTokenAccessGuard.IsHiddenVariantTokenAsync(storedToken, cancellationToken)))
            {
                throw new KeyNotFoundException($"Shift with ID {request.Id} not found");
            }

            if (shift.ScenarioSourceShiftId != null
                || (shift.AnalyseToken is { } token
                    && !await _scenarioTokenAccessGuard.IsVariantTokenVisibleAsync(token, cancellationToken)))
            {
                throw new InvalidOperationException(
                    $"Shift {request.Id} is a scenario clone and cannot be fetched in real mode.");
            }

            return _scheduleMapper.ToShiftResource(shift);
        }, nameof(Handle), new { request.Id });
    }
}
