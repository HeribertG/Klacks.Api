// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for creating a variant (key): an analyse_scenarios row of kind Variant with its own token that only carries
/// not yet active shifts. Name, group and period are validated, a hidden group is answered like a missing one.
/// </summary>
/// <param name="Request">Contains name, description, group and time period of the variant</param>
/// <param name="scenarioRepository">Stores the variant row</param>
/// <param name="groupVisibilityGuard">Refuses a hidden target group like a missing one</param>
/// <param name="companyClock">Gives today in the company's time zone for the expiry flag</param>

using Klacks.Api.Application.Commands.ScenarioVariants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScenarioVariants;

public class CreateScenarioVariantCommandHandler : BaseHandler, IRequestHandler<CreateScenarioVariantCommand, ScenarioVariantResource>
{
    private const int NewVariantShiftCount = 0;

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly ICompanyClock _companyClock;

    public CreateScenarioVariantCommandHandler(
        IAnalyseScenarioRepository scenarioRepository,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        ICompanyClock companyClock,
        ILogger<CreateScenarioVariantCommandHandler> logger)
        : base(logger)
    {
        _scenarioRepository = scenarioRepository;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
        _companyClock = companyClock;
    }

    public async Task<ScenarioVariantResource> Handle(CreateScenarioVariantCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var request = command.Request;

            ScenarioVariantValidation.EnsureGroup(request.GroupId);
            ScenarioVariantValidation.EnsureDetails(request.Name, request.FromDate, request.UntilDate);
            await _groupVisibilityGuard.EnsureScenarioScopeWritableAsync(request.GroupId, cancellationToken);

            var variant = new AnalyseScenario
            {
                Kind = AnalyseScenarioKind.Variant,
                Token = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                GroupId = request.GroupId,
                FromDate = request.FromDate,
                UntilDate = request.UntilDate
            };

            await _scenarioRepository.Add(variant);
            await _unitOfWork.CompleteAsync();

            var today = await _companyClock.GetTodayDateAsync(cancellationToken);

            return ScenarioVariantResourceFactory.Create(variant, NewVariantShiftCount, today);
        }, nameof(Handle), new { command.Request.Name, command.Request.GroupId });
    }
}
