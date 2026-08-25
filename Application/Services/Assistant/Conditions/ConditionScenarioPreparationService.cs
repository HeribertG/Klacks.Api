// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IConditionScenarioPreparationService"/>. Follows the same three steps a human's
/// scenario takes (CoverAbsenceCommandHandler): stage the AnalyseScenario, clone the schedule data
/// under its token, commit once. The one addition is CreatedByUser - nothing stamps that column
/// automatically, and a scenario created outside an HTTP request would otherwise be authored by
/// DataBaseContext's "Anonymous" fallback, hiding that Klacksy made it.
///
/// The commit sits between the scenario and the ledger transition on purpose: the ledger repository
/// opens its own database transaction, so it must not run while a unit of work is still open. Losing
/// the compare-and-swap after that point means another instance prepared its own scenario for the same
/// finding first, which makes this one the orphan - it is soft-deleted again rather than left to age
/// into a "scenario pending" alert nobody can explain.
/// </summary>
/// <param name="scenarioRepository">Stages the scenario row (stage-only, commits through the unit of work).</param>
/// <param name="scenarioService">Clones the schedule data under the scenario token, likewise stage-only.</param>
/// <param name="unitOfWork">The single commit for scenario plus clones.</param>
/// <param name="ledgerService">Moves the condition to Prepared and links the scenario, in its own transaction.</param>
/// <param name="triggerService">Delivers the "a scenario is waiting" note through the ordinary proactive pipeline.</param>
/// <param name="audienceResolver">Resolves which planners may see this finding, group-scoped where the finding names a group.</param>
/// <param name="timeProvider">Stamps HandledAtUtc on the ledger row.</param>
/// <param name="logger">Records discarded scenarios and failed notifications.</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class ConditionScenarioPreparationService : IConditionScenarioPreparationService
{
    private const string ScenarioNameFormat = "{0} {1} {2:yyyy-MM-dd}";
    private const string PreparedDetailFormat = "scenario {0}";

    private const string DiscardFailedMessage =
        "Condition {ConditionId} lost the transition to Prepared and its orphaned scenario {ScenarioId} could not be discarded; it stays visible to planners";

    private const string LedgerConflictMessage =
        "Condition {ConditionId} was moved by another instance before it could be marked Prepared; scenario {ScenarioId} was discarded again";

    private const string NotificationFailedMessage =
        "Condition {ConditionId} is Prepared with scenario {ScenarioId}, but the planners could not be notified";

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly IAgentTriggerService _triggerService;
    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConditionScenarioPreparationService> _logger;

    public ConditionScenarioPreparationService(
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IAgentConditionLedgerService ledgerService,
        IAgentTriggerService triggerService,
        IPlanningAudienceResolver audienceResolver,
        TimeProvider timeProvider,
        ILogger<ConditionScenarioPreparationService> logger)
    {
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _ledgerService = ledgerService;
        _triggerService = triggerService;
        _audienceResolver = audienceResolver;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ConditionScenarioPreparationResult> PrepareScenarioForConditionAsync(
        AgentCondition condition,
        ConditionScenarioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(request);

        if (condition.Status == AgentConditionStatus.Prepared)
        {
            return new ConditionScenarioPreparationResult(
                ConditionScenarioPreparationOutcome.AlreadyPrepared, condition.ScenarioId, null);
        }

        if (condition.Status != AgentConditionStatus.Reported)
        {
            return new ConditionScenarioPreparationResult(
                ConditionScenarioPreparationOutcome.NotPreparable, null, null);
        }

        var groupId = request.GroupId ?? condition.GroupId;
        var scenario = await CreateScenarioAsync(condition, request, groupId, cancellationToken);

        var moved = await _ledgerService.TryTransitionAsync(
            condition.Id,
            AgentConditionStatus.Reported,
            AgentConditionStatus.Prepared,
            userId: null,
            detail: string.Format(CultureInfo.InvariantCulture, PreparedDetailFormat, scenario.Id),
            fields: new AgentConditionTransitionFields(
                HandledAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
                ScenarioId: scenario.Id,
                HandlingKind: AgentConditionHandlingKind.ScenarioPrepared),
            cancellationToken);

        if (!moved)
        {
            _logger.LogWarning(LedgerConflictMessage, condition.Id, scenario.Id);
            await DiscardScenarioAsync(scenario, condition.Id, cancellationToken);

            return new ConditionScenarioPreparationResult(
                ConditionScenarioPreparationOutcome.LedgerConflict, null, null);
        }

        await NotifyPlannersAsync(condition, scenario, groupId, cancellationToken);

        return new ConditionScenarioPreparationResult(
            ConditionScenarioPreparationOutcome.Prepared, scenario.Id, scenario.Token);
    }

    private async Task<AnalyseScenario> CreateScenarioAsync(
        AgentCondition condition,
        ConditionScenarioRequest request,
        Guid? groupId,
        CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid();
        var scenario = new AnalyseScenario
        {
            Name = request.Name ?? BuildScenarioName(condition, request),
            GroupId = groupId,
            FromDate = request.FromDate,
            UntilDate = request.UntilDate,
            Token = token,
            RunGroupId = Guid.NewGuid(),
            CreatedByUser = KlacksyIdentity.SystemUserName
        };

        await _scenarioRepository.Add(scenario);
        await _scenarioService.CloneScenarioDataWithMapsAsync(
            groupId, request.FromDate, request.UntilDate, token, additionalShiftIds: null, cancellationToken);
        await _unitOfWork.CompleteAsync();

        return scenario;
    }

    private static string BuildScenarioName(AgentCondition condition, ConditionScenarioRequest request) =>
        string.Format(
            CultureInfo.InvariantCulture,
            ScenarioNameFormat,
            KlacksyIdentity.SystemUserName,
            condition.TriggerKind,
            request.FromDate);

    private async Task DiscardScenarioAsync(
        AnalyseScenario scenario, Guid conditionId, CancellationToken cancellationToken)
    {
        try
        {
            await _scenarioService.SoftDeleteScenarioDataAsync(scenario.Token, cancellationToken);
            await _scenarioRepository.Delete(scenario.Id);
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, DiscardFailedMessage, conditionId, scenario.Id);
        }
    }

    /// <summary>
    /// One targeted event per planner instead of a single PlannersOnly broadcast - see
    /// ScenarioPreparedTriggerEvent for why the broadcast shape would open a ledger row nothing ever
    /// closes. A failure here is logged and swallowed: the transition to Prepared has already been
    /// committed and the proposal is reachable in the schedule, so undoing the preparation over an
    /// undelivered note would cost more than the note is worth.
    /// </summary>
    private async Task NotifyPlannersAsync(
        AgentCondition condition, AnalyseScenario scenario, Guid? groupId, CancellationToken cancellationToken)
    {
        try
        {
            var recipients = groupId is Guid scopedGroupId
                ? await _audienceResolver.GetPlanningUserIdsForGroupAsync(scopedGroupId, cancellationToken)
                : await _audienceResolver.GetPlanningUserIdsAsync(cancellationToken);

            foreach (var recipient in recipients)
            {
                if (!Guid.TryParse(recipient, out var recipientId))
                {
                    continue;
                }

                await _triggerService.OnEventAsync(
                    new ScenarioPreparedTriggerEvent(
                        scenario.Id, scenario.Name, groupId, condition.TriggerKind, recipientId),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, NotificationFailedMessage, condition.Id, scenario.Id);
        }
    }
}
