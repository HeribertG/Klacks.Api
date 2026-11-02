// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for deleting a variant (key). A variant of a hidden group is answered like a missing one; a consumed variant
/// and a variant that an Active scenario still carries are answered with the conflicts of ScenarioVariantAccess. Otherwise the token-scoped
/// soft-delete of the variant's shifts runs first, then the variant row and the links of finished scenarios are
/// soft-deleted.
/// </summary>
/// <param name="Id">Row id of the variant to delete</param>
/// <param name="variantRepository">Loads the tracked variant and stages the link soft-delete</param>
/// <param name="scenarioRepository">Soft-deletes the variant row</param>
/// <param name="scenarioService">Soft-deletes every row that carries the variant token</param>
/// <param name="groupVisibilityGuard">Answers a variant outside the caller's group visibility like a missing one</param>

using Klacks.Api.Application.Commands.ScenarioVariants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScenarioVariants;

public class DeleteScenarioVariantCommandHandler : BaseHandler, IRequestHandler<DeleteScenarioVariantCommand, bool>
{
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public DeleteScenarioVariantCommandHandler(
        IScenarioVariantRepository variantRepository,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<DeleteScenarioVariantCommandHandler> logger)
        : base(logger)
    {
        _variantRepository = variantRepository;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<bool> Handle(DeleteScenarioVariantCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var variant = await ScenarioVariantAccess.LoadEditableAsync(
                _variantRepository, _groupVisibilityGuard, command.Id, cancellationToken);

            await _scenarioService.SoftDeleteScenarioDataAsync(variant.Token, cancellationToken);
            await _scenarioRepository.Delete(variant.Id);
            await _variantRepository.SoftDeleteFinishedScenarioLinksAsync(variant.Id, cancellationToken);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(Handle), new { command.Id });
    }
}
