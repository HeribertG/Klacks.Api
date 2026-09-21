// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores an administrator's advance approval for one trigger kind in one scope, after five gates that
/// each close a way a grant could look effective while being unable to act - or could act further than
/// the Owner's floor allows:
/// (1) the kind must have a remediation in the code-only ConditionRemediationRegistry, since a kind
///     without one is capped at Hint and could never execute anything;
/// (2) that remediation's skill must still be registered, because an unknown skill has neither
///     permissions nor a risk class to judge;
/// (3) its risk class must be one the unattended path may ever run: Irreversible and Sensitive are
///     refused here, matching the floor UnattendedSkillPolicy enforces again at execution time - this is
///     the honest early error, never the authoritative gate;
/// (4) the granting administrator must hold the skill's own RequiredPermissions right now (Admin bypass,
///     through the same ISkillPermissionGate the approval roster is filtered with), because those are
///     the rights the execution will borrow from them;
/// (5) their own autonomy level must clear the threshold the unattended policy applies to that risk
///     class, since an admin who lowered their level would otherwise get a grant whose every execution
///     is refused, visible only in a warning log.
/// A running grant for the same kind and scope is reported as a COLLISION and never silently replaced:
/// replacing would move an expiry or widen a budget with no record of who ended the previous window, so
/// an administrator has to revoke it first and both rows stay in the audit trail.
/// </summary>
/// <param name="repository">Stores the grant; stage-only, this handler commits.</param>
/// <param name="unitOfWork">Commits the staged row.</param>
/// <param name="remediationRegistry">Which skill remediates the kind, and whether one exists at all.</param>
/// <param name="skillRegistry">Source of that skill's descriptor and RequiredPermissions.</param>
/// <param name="riskClassifier">Current risk class of the remediation skill.</param>
/// <param name="permissionGate">Whether the granting administrator holds those permissions right now.</param>
/// <param name="autonomyRepository">The granter's autonomy level; a missing row falls back to AutonomyDefaults.DefaultLevel.</param>
/// <param name="timeProvider">Clock the grant and its expiry are stamped from.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GrantStandingApprovalCommandHandler
    : IRequestHandler<GrantStandingApprovalCommand, GrantStandingApprovalResult>
{
    private const string NoRemediationReason =
        "Klacksy has no remediation for '{0}', so a standing approval for it could never execute anything.";

    private const string UnknownSkillReason =
        "The remediation skill '{0}' is not registered, so its permissions and its risk cannot be judged.";

    private const string NotReversibleReason =
        "The remediation '{0}' is classified as {1}. A standing approval only ever covers remediations that "
        + "can be undone; an irreversible or sensitive action has to be approved per finding.";

    private const string MissingPermissionsReason =
        "A standing approval runs '{0}' under your own rights, and you do not hold the permissions it "
        + "requires.";

    private const string AutonomyTooLowReason =
        "A standing approval runs '{0}' unattended, which needs autonomy level {1} or higher; yours is {2}. "
        + "Raise your autonomy level first, otherwise every execution under the grant would be refused.";

    private const string AlreadyActiveReason =
        "A standing approval for this kind and scope is already active until {0:u} (granted {1:u}). Revoke it "
        + "before granting a new one.";

    private const string DurationOutOfRangeReason =
        "The duration has to be between {0} and {1} day(s).";

    private const string BudgetOutOfRangeReason =
        "The daily budget has to be between {0} and {1}.";

    private readonly IStandingApprovalRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConditionRemediationRegistry _remediationRegistry;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillRiskClassifier _riskClassifier;
    private readonly ISkillPermissionGate _permissionGate;
    private readonly IAgentAutonomyPreferenceRepository _autonomyRepository;
    private readonly TimeProvider _timeProvider;

    public GrantStandingApprovalCommandHandler(
        IStandingApprovalRepository repository,
        IUnitOfWork unitOfWork,
        IConditionRemediationRegistry remediationRegistry,
        ISkillRegistry skillRegistry,
        ISkillRiskClassifier riskClassifier,
        ISkillPermissionGate permissionGate,
        IAgentAutonomyPreferenceRepository autonomyRepository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _remediationRegistry = remediationRegistry;
        _skillRegistry = skillRegistry;
        _riskClassifier = riskClassifier;
        _permissionGate = permissionGate;
        _autonomyRepository = autonomyRepository;
        _timeProvider = timeProvider;
    }

    public async Task<GrantStandingApprovalResult> Handle(
        GrantStandingApprovalCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var durationDays = request.DurationDays ?? StandingApprovalDefaults.DefaultDurationDays;
        if (durationDays < StandingApprovalDefaults.MinimumDurationDays
            || durationDays > StandingApprovalDefaults.MaximumDurationDays)
        {
            return GrantStandingApprovalResult.Refused(string.Format(
                CultureInfo.InvariantCulture,
                DurationOutOfRangeReason,
                StandingApprovalDefaults.MinimumDurationDays,
                StandingApprovalDefaults.MaximumDurationDays));
        }

        var dailyBudget = request.DailyBudget ?? StandingApprovalDefaults.DefaultDailyBudget;
        if (dailyBudget < StandingApprovalDefaults.MinimumDailyBudget
            || dailyBudget > StandingApprovalDefaults.MaximumDailyBudget)
        {
            return GrantStandingApprovalResult.Refused(string.Format(
                CultureInfo.InvariantCulture,
                BudgetOutOfRangeReason,
                StandingApprovalDefaults.MinimumDailyBudget,
                StandingApprovalDefaults.MaximumDailyBudget));
        }

        var refusal = await CheckRemediationAsync(request, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        var running = await _repository.FindActiveAsync(
            request.TriggerKind, request.GroupId, nowUtc, cancellationToken);
        if (running is not null)
        {
            return GrantStandingApprovalResult.AlreadyActive(string.Format(
                CultureInfo.InvariantCulture, AlreadyActiveReason, running.ExpiresAtUtc, running.GrantedAtUtc));
        }

        var approval = new StandingApproval
        {
            Id = Guid.NewGuid(),
            TriggerKind = request.TriggerKind,
            GroupId = request.GroupId,
            GrantedByUserId = request.GrantedByUserId,
            GrantedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.AddDays(durationDays),
            DailyBudget = dailyBudget
        };

        await _repository.AddAsync(approval, cancellationToken);
        await _unitOfWork.CompleteAsync();

        return GrantStandingApprovalResult.Granted(StandingApprovalDtoMapper.ToDto(approval, nowUtc));
    }

    private async Task<GrantStandingApprovalResult?> CheckRemediationAsync(
        GrantStandingApprovalCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_remediationRegistry.TryGetEntry(request.TriggerKind, out var entry) || entry is null)
        {
            return GrantStandingApprovalResult.Refused(string.Format(
                CultureInfo.InvariantCulture, NoRemediationReason, request.TriggerKind));
        }

        var descriptor = _skillRegistry.GetSkillByName(entry.RemediationSkillName);
        if (descriptor is null)
        {
            return GrantStandingApprovalResult.Refused(string.Format(
                CultureInfo.InvariantCulture, UnknownSkillReason, entry.RemediationSkillName));
        }

        var riskClass = _riskClassifier.Classify(descriptor);
        if (!IsCoveredRiskClass(riskClass))
        {
            return GrantStandingApprovalResult.Refused(string.Format(
                CultureInfo.InvariantCulture, NotReversibleReason, entry.RemediationSkillName, riskClass));
        }

        if (!await _permissionGate.HoldsAsync(
            request.GrantedByUserId.ToString(), descriptor.RequiredPermissions))
        {
            return GrantStandingApprovalResult.Refused(string.Format(
                CultureInfo.InvariantCulture, MissingPermissionsReason, entry.RemediationSkillName));
        }

        var minimumLevel = MinimumAutonomyLevelFor(riskClass);
        var level = await GetAutonomyLevelAsync(request.GrantedByUserId, cancellationToken);
        if (level < minimumLevel)
        {
            return GrantStandingApprovalResult.Refused(string.Format(
                CultureInfo.InvariantCulture,
                AutonomyTooLowReason, entry.RemediationSkillName, minimumLevel, level));
        }

        return null;
    }

    /// <summary>
    /// The risk classes a standing approval may cover at all. ReadOnly and Reversible are the Owner's
    /// floor; ScenarioGated is included because it writes into an AnalyseScenario a human still has to
    /// accept, which is strictly less than a reversible live change. Irreversible, Sensitive and any
    /// class this code does not know are refused.
    /// </summary>
    private static bool IsCoveredRiskClass(SkillRiskClass riskClass) =>
        riskClass is SkillRiskClass.ReadOnly or SkillRiskClass.Reversible or SkillRiskClass.ScenarioGated;

    /// <summary>
    /// The threshold UnattendedSkillPolicy will apply to this risk class at execution time. Mirrored
    /// here on purpose and kept to the two classes that have one, so the refusal names the same number
    /// the later gate would; a class without a threshold falls back to the lowest level, which every
    /// account clears. The pairing is pinned by GrantStandingApprovalCommandHandlerTests.
    /// </summary>
    private static AutonomyLevel MinimumAutonomyLevelFor(SkillRiskClass riskClass) => riskClass switch
    {
        SkillRiskClass.Reversible => UnattendedSkillPolicyDefaults.MinimumLevelForReversible,
        SkillRiskClass.ScenarioGated => UnattendedSkillPolicyDefaults.MinimumLevelForScenarioGated,
        _ => AutonomyDefaults.MinimumLevel
    };

    private async Task<AutonomyLevel> GetAutonomyLevelAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _autonomyRepository.GetAsync(userId.ToString(), cancellationToken);
        return row?.Level ?? AutonomyDefaults.DefaultLevel;
    }
}
