// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Writes one governance rule, the global kill switch and/or the global autonomy level, then answers
/// with the complete new picture. Validation runs on the MERGED row, never on the incoming patch: raising MaxAction to
/// Prepare on a row that has no responsible owner, and clearing the owner of a row that already sits
/// at Prepare, are the same violation and both have to fail. The rule is that from Prepare upwards a
/// human must be named, because Etappe 4d issues an internal token for exactly that account and acts
/// under their current roles - an action with nobody accountable for it must not be reachable. It is
/// enforced here rather than as a database CHECK because it spans two columns and has to fail with a
/// readable message.
/// </summary>
/// <param name="repository">Stores the governance rules.</param>
/// <param name="settingsRepository">Persists the global kill-switch setting row.</param>
/// <param name="unitOfWork">Spans both writes in one transaction; see the remarks.</param>
/// <param name="resolver">Reads back the effective governance for the answer.</param>
/// <param name="userManager">Verifies that a named responsible owner really exists.</param>
/// <remarks>
/// The two writes follow the project's two different SaveChanges conventions: ISettingsRepository is
/// stage-only and never saves by itself, while IAgentTriggerGovernanceRepository commits on its own.
/// Mixing them unguarded is the documented trap - a lone kill-switch change would never reach the
/// database, and a combined request would have the governance repository's SaveChangesAsync flush the
/// staged settings row as an accidental side effect. ExecuteInTransactionAsync spans both, and the
/// explicit CompleteAsync flushes the staged row even when no rule write follows. It also makes the
/// request atomic: a rule patch that fails validation rolls the kill-switch change back with it.
/// </remarks>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SetProactiveGovernanceCommandHandler
    : IRequestHandler<SetProactiveGovernanceCommand, ProactiveGovernanceDto>
{
    private const int MinimumBudget = 0;
    private const int MinimumWindowMinutes = 1;
    private const string TrueValue = "true";
    private const string FalseValue = "false";

    private readonly IAgentTriggerGovernanceRepository _repository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProactiveGovernanceResolver _resolver;
    private readonly UserManager<AppUser> _userManager;

    public SetProactiveGovernanceCommandHandler(
        IAgentTriggerGovernanceRepository repository,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        IProactiveGovernanceResolver resolver,
        UserManager<AppUser> userManager)
    {
        _repository = repository;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _resolver = resolver;
        _userManager = userManager;
    }

    public async Task<ProactiveGovernanceDto> Handle(
        SetProactiveGovernanceCommand request, CancellationToken cancellationToken)
    {
        if (request.TriggerKind is null && request.KillSwitch is null && request.AutonomyLevel is null)
        {
            throw new InvalidRequestException(
                "Nothing to change: supply a triggerKind to change a rule, killSwitch to change the " +
                "global switch, autonomyLevel to change the global cap, or any combination.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (request.KillSwitch is bool killSwitch)
            {
                await _settingsRepository.UpsertSettingAsync(
                    SettingKeys.KlacksyProactiveKillSwitch, killSwitch ? TrueValue : FalseValue);
                await _unitOfWork.CompleteAsync();
            }

            if (request.AutonomyLevel is AutonomyLevel level)
            {
                if (!Enum.IsDefined(level))
                {
                    throw new InvalidRequestException($"Unknown autonomyLevel value '{(int)level}'.");
                }

                await _settingsRepository.UpsertSettingAsync(
                    SettingKeys.KlacksyProactiveAutonomyLevel, ((int)level).ToString());
                await _unitOfWork.CompleteAsync();
            }

            if (request.TriggerKind is string triggerKind)
            {
                await WriteRuleAsync(triggerKind, request, cancellationToken);
            }

            return true;
        });

        var killSwitchActive = await _resolver.IsKillSwitchActiveAsync(cancellationToken);
        var globalAutonomyLevel = await _resolver.GetGlobalAutonomyLevelAsync(cancellationToken);
        var decisions = await _resolver.ResolveAllAsync(cancellationToken);
        return ProactiveGovernanceDtoMapper.ToDto(killSwitchActive, globalAutonomyLevel, decisions);
    }

    private async Task WriteRuleAsync(
        string triggerKind, SetProactiveGovernanceCommand request, CancellationToken cancellationToken)
    {
        if (!ProactiveGovernanceDefaults.IsGovernedKind(triggerKind))
        {
            throw new InvalidRequestException(
                $"Trigger kind '{triggerKind}' cannot be governed. Only kinds that reach the condition " +
                "ledger have a governance rule.");
        }

        var merged = await _repository.FindAsync(triggerKind, request.GroupId, cancellationToken)
            ?? new AgentTriggerGovernance { TriggerKind = triggerKind, GroupId = request.GroupId };

        ApplyPatch(merged, request);
        await ValidateMergedAsync(merged, cancellationToken);

        await _repository.UpsertAsync(merged, cancellationToken);
    }

    private static void ApplyPatch(AgentTriggerGovernance merged, SetProactiveGovernanceCommand request)
    {
        if (request.MaxAction is ProactiveMaxAction maxAction)
        {
            merged.MaxAction = maxAction;
        }

        if (request.Enabled is bool enabled)
        {
            merged.Enabled = enabled;
        }

        if (request.ClearResponsibleOwner)
        {
            merged.ResponsibleOwnerUserId = null;
        }
        else if (request.ResponsibleOwnerUserId is Guid ownerUserId)
        {
            merged.ResponsibleOwnerUserId = ownerUserId;
        }

        if (request.DailyActionBudget is int dailyActionBudget)
        {
            merged.DailyActionBudget = dailyActionBudget;
        }

        if (request.WindowActionLimit is int windowActionLimit)
        {
            merged.WindowActionLimit = windowActionLimit;
        }

        if (request.WindowMinutes is int windowMinutes)
        {
            merged.WindowMinutes = windowMinutes;
        }
    }

    private async Task ValidateMergedAsync(
        AgentTriggerGovernance merged, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(merged.MaxAction))
        {
            throw new InvalidRequestException($"Unknown maxAction value '{(int)merged.MaxAction}'.");
        }

        if (merged.DailyActionBudget < MinimumBudget || merged.WindowActionLimit < MinimumBudget)
        {
            throw new InvalidRequestException(
                $"dailyActionBudget and windowActionLimit must not be below {MinimumBudget}.");
        }

        if (merged.WindowMinutes < MinimumWindowMinutes)
        {
            throw new InvalidRequestException($"windowMinutes must be at least {MinimumWindowMinutes}.");
        }

        if (merged.MaxAction < ProactiveMaxAction.Prepare)
        {
            return;
        }

        if (merged.ResponsibleOwnerUserId is not Guid ownerUserId)
        {
            throw new InvalidRequestException(
                $"A responsible owner is required from maxAction {nameof(ProactiveMaxAction.Prepare)} " +
                "upwards, because the prepared action runs under that person's identity.");
        }

        var owner = await _userManager.FindByIdAsync(ownerUserId.ToString());
        if (owner is null)
        {
            throw new InvalidRequestException(
                $"The responsible owner '{ownerUserId}' does not exist.");
        }
    }
}
