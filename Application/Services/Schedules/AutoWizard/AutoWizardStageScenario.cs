// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.AutoWizard;

/// <summary>
/// One scenario a stage of the AutoWizard chain produced.
/// </summary>
/// <param name="Stage">Stage name (Wizard, Harmonizer, HolisticHarmonizer).</param>
/// <param name="ScenarioId">Id of the scenario.</param>
/// <param name="Token">Analyse token of the scenario.</param>
/// <param name="Name">Display name of the scenario.</param>
public sealed record AutoWizardStageScenario(string Stage, Guid ScenarioId, Guid Token, string Name);
