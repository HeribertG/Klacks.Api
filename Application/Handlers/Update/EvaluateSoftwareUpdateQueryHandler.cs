// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Gathers the facts of the software update advice and hands them to SoftwareUpdateAdvisor. The active operation comes
/// from the trigger's preflight, not from the update status, because the status hides third-party operations the trigger
/// still refuses on. Autofill runs are counted unfiltered: the question is what a restart ends for the whole installation,
/// and only counts leave this handler, never a group. Read-only; never enqueues work.
/// </summary>
/// <param name="preflight">The trigger's own decision</param>
/// <param name="repository">Reads the newest Klacks update or rollback</param>
/// <param name="trustOptions">Tells whether the deployment configures a release address</param>
/// <param name="autofillRuns">Autofill runs in progress</param>
/// <param name="wizardJobs">Running wizard jobs</param>
/// <param name="harmonizerJobs">Running harmonizer jobs</param>
/// <param name="holisticHarmonizerJobs">Running holistic harmonizer jobs</param>
/// <param name="connectedUsers">Users with an open assistant connection</param>
/// <param name="timeProvider">Clock of the evaluation</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Application.Queries.Update;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Services.Update;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Handlers.Update;

public class EvaluateSoftwareUpdateQueryHandler : IRequestHandler<EvaluateSoftwareUpdateQuery, SoftwareUpdateEvaluation>
{
    private readonly IUpdateTriggerPreflight _preflight;
    private readonly IUpdateHistoryRepository _repository;
    private readonly UpdateTrustOptions _trustOptions;
    private readonly IAutofillRunPresenceTracker _autofillRuns;
    private readonly WizardJobRegistry _wizardJobs;
    private readonly HarmonizerJobRegistry _harmonizerJobs;
    private readonly HolisticHarmonizerJobRegistry _holisticHarmonizerJobs;
    private readonly IConnectedUserIdReader _connectedUsers;
    private readonly TimeProvider _timeProvider;

    public EvaluateSoftwareUpdateQueryHandler(
        IUpdateTriggerPreflight preflight,
        IUpdateHistoryRepository repository,
        IOptions<UpdateTrustOptions> trustOptions,
        IAutofillRunPresenceTracker autofillRuns,
        WizardJobRegistry wizardJobs,
        HarmonizerJobRegistry harmonizerJobs,
        HolisticHarmonizerJobRegistry holisticHarmonizerJobs,
        IConnectedUserIdReader connectedUsers,
        TimeProvider timeProvider)
    {
        _preflight = preflight;
        _repository = repository;
        _trustOptions = trustOptions.Value;
        _autofillRuns = autofillRuns;
        _wizardJobs = wizardJobs;
        _harmonizerJobs = harmonizerJobs;
        _holisticHarmonizerJobs = holisticHarmonizerJobs;
        _connectedUsers = connectedUsers;
        _timeProvider = timeProvider;
    }

    public async Task<SoftwareUpdateEvaluation> Handle(EvaluateSoftwareUpdateQuery request, CancellationToken cancellationToken)
    {
        var preflight = await _preflight.CheckAsync(cancellationToken);
        var last = await _repository.GetLatestByTypesAsync(
            KlacksSelfUpdateTypes.All, cancellationToken);
        var connected = await _connectedUsers.GetConnectedUserIdsAsync(cancellationToken);

        return SoftwareUpdateAdvisor.Evaluate(new SoftwareUpdateFacts(
            preflight,
            !string.IsNullOrWhiteSpace(_trustOptions.ManifestBaseUrl),
            last,
            new SoftwareUpdateRunningWork(
                _autofillRuns.GetActive().Count,
                _wizardJobs.RunningCount,
                _harmonizerJobs.RunningCount,
                _holisticHarmonizerJobs.RunningCount),
            connected.Count(userId => !string.Equals(userId, request.CallerId, StringComparison.OrdinalIgnoreCase)),
            _timeProvider.GetUtcNow().UtcDateTime));
    }
}
