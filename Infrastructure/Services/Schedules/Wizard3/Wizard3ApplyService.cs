// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Schedules.Wizard3;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.Wizard3;

/// <summary>
/// Wizard-3-specific apply service. Reuses the full <see cref="HarmonizerApplyService"/>
/// pipeline (cache lookup, RunGroupId inheritance, Bitmap → Work conversion, scenario clone,
/// BulkAddWorks) and only changes the scenario name prefix from "Harmonisiert" to "LLM" so
/// the operator can distinguish Wizard 3 outputs in the scenario list.
/// </summary>
public sealed class Wizard3ApplyService : HarmonizerApplyService, IWizard3ApplyService
{
    protected override string ScenarioNamePrefix => "LLM";

    public Wizard3ApplyService(
        HarmonizerResultCache resultCache,
        IMediator mediator,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        DataBaseContext context,
        ILogger<HarmonizerApplyService> logger)
        : base(resultCache, mediator, scenarioRepository, scenarioService, unitOfWork, context, logger)
    {
    }
}
