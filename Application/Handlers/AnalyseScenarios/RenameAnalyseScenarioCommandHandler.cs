// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for renaming an AnalyseScenario. Updates only the name field,
/// leaving all scenario data (token, dates, status) unchanged.
/// </summary>
/// <param name="Id">ID of the scenario to rename</param>
/// <param name="Name">New name for the scenario</param>
/// <param name="variantRepository">Reads the variants (keys) the scenario carries</param>
/// <param name="groupVisibilityGuard">Answers a scenario outside the caller's group visibility like a missing one</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class RenameAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<RenameAnalyseScenarioCommand, AnalyseScenarioResource>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public RenameAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IScenarioVariantRepository variantRepository,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<RenameAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<AnalyseScenarioResource> Handle(RenameAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _groupVisibilityGuard.EnsureScenarioVisibleAsync(
                await _repository.Get(command.Id), command.Id, cancellationToken);

            scenario.Name = command.Name;

            await _repository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            var variantIds = await _variantRepository.GetVariantIdsAsync(scenario.Token, cancellationToken);

            return AnalyseScenarioResourceFactory.ToResource(scenario, variantIds);
        }, nameof(Handle), new { command.Id, command.Name });
    }
}
