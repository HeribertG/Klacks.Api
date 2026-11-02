// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for creating a new AnalyseScenario. Stores the scenario row and
/// delegates the full schedule clone (shifts, works with ParentWorkId
/// mapping, work_changes, expenses, breaks, schedule_notes) to
/// <see cref="IAnalyseScenarioService"/>. Selected variants (keys) are checked before anything is staged; every shift of
/// a key joins the clone, and the links land in the same commit.
/// </summary>
/// <param name="Request">Contains name, description, optional group, time period and the selected variants</param>
/// <param name="variantRepository">Loads the selected variants and their shifts and stages the scenario links</param>
/// <param name="groupVisibilityGuard">Refuses a hidden target group like a missing one and a group-less scenario for restricted callers</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class CreateAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<CreateAnalyseScenarioCommand, AnalyseScenarioResource>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public CreateAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IScenarioVariantRepository variantRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<CreateAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _variantRepository = variantRepository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<AnalyseScenarioResource> Handle(CreateAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var token = Guid.NewGuid();
            var fromDate = command.Request.FromDate;
            var untilDate = command.Request.UntilDate;
            var groupId = command.Request.GroupId;

            await _groupVisibilityGuard.EnsureScenarioScopeWritableAsync(groupId, cancellationToken);

            var variants = await ScenarioVariantSelection.LoadSelectedAsync(
                _variantRepository, _scenarioService, _groupVisibilityGuard, groupId, command.Request.VariantIds, cancellationToken);

            var scenario = new AnalyseScenario
            {
                Name = command.Request.Name,
                Description = command.Request.Description,
                GroupId = groupId,
                FromDate = fromDate,
                UntilDate = untilDate,
                Token = token
            };

            await _repository.Add(scenario);

            var variantIds = variants.Select(variant => variant.Id).ToList();
            if (variants.Count == 0)
            {
                await _scenarioService.CloneScenarioDataAsync(groupId, fromDate, untilDate, token, additionalShiftIds: null, sourceToken: null, cancellationToken);
            }
            else
            {
                var variantTokens = variants.Select(variant => variant.Token).ToHashSet();
                var keyShiftIds = await _variantRepository.GetShiftIdsByVariantTokensAsync(variantTokens, cancellationToken);
                await _scenarioService.CloneScenarioDataAsync(groupId, fromDate, untilDate, token, keyShiftIds, sourceToken: null, cancellationToken, variantTokens);
                await _variantRepository.AddLinksAsync(token, variantIds, cancellationToken);
            }

            await _unitOfWork.CompleteAsync();

            return AnalyseScenarioResourceFactory.ToResource(scenario, variantIds);
        }, nameof(Handle), new { command.Request.Name, command.Request.GroupId });
    }
}
