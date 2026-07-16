// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reverts an applied company rule. SurchargeSettings writes the snapshotted setting values back (keys
/// that did not exist before the apply cannot be removed because settings have no delete path, and are
/// left untouched); a snapshot key outside <see cref="CompanyRuleSurchargeSettingMap"/> is skipped and
/// logged rather than written, as defense-in-depth against a tampered or corrupted snapshot. CounterRule
/// and CustomMacro soft-delete the target entity they created, tolerating a target that is already gone.
/// In every case the registry row is soft-deleted. When restoring the snapshot actually changed at least
/// one surcharge-relevant setting value (see SurchargeRelevantSettingKeys), a
/// SurchargeSettingsChangedEvent is dispatched post-commit and defensively so persisted work surcharges
/// are recalculated. A genuine delete blocker (for example a macro still referenced by shifts) is
/// surfaced as its real message. The list of changed surcharge keys is cleared at the start of every
/// snapshot restore so that an execution-strategy retry of the transaction cannot accumulate duplicate
/// keys.
/// </summary>
/// <param name="request">Identifies the registry row to revert.</param>

using System.Text.Json;
using Klacks.Api.Application.Commands.CompanyRules;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Mediator;
using MacroCommands = Klacks.Api.Application.Commands.Settings.Macros;

namespace Klacks.Api.Application.Handlers.CompanyRules;

public class RevertCompanyRuleCommandHandler : IRequestHandler<RevertCompanyRuleCommand, CompanyRuleResource?>
{
    private readonly ICompanyRuleRepository _companyRuleRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevertCompanyRuleCommandHandler> _logger;
    private readonly SettingsMapper _mapper;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RevertCompanyRuleCommandHandler(
        ICompanyRuleRepository companyRuleRepository,
        ISettingsRepository settingsRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ILogger<RevertCompanyRuleCommandHandler> logger,
        SettingsMapper mapper,
        IDomainEventDispatcher eventDispatcher)
    {
        _companyRuleRepository = companyRuleRepository;
        _settingsRepository = settingsRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<CompanyRuleResource?> Handle(RevertCompanyRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _companyRuleRepository.GetAsync(request.Id);
        if (rule is null)
        {
            throw new InvalidRequestException($"No applied company rule found with id '{request.Id}'.");
        }

        var resource = _mapper.ToCompanyRuleResource(rule);
        var changedSurchargeKeys = new List<string>();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            switch (rule.Kind)
            {
                case CompanyRuleKind.SurchargeSettings:
                    await RestoreSurchargeSnapshotAsync(rule, changedSurchargeKeys);
                    break;
                case CompanyRuleKind.CounterRule:
                    await RevertCounterRuleAsync(rule, cancellationToken);
                    break;
                case CompanyRuleKind.CustomMacro:
                    await RevertCustomMacroAsync(rule, cancellationToken);
                    break;
            }

            await _companyRuleRepository.DeleteAsync(rule.Id);
            await _unitOfWork.CompleteAsync();
            return true;
        });

        if (changedSurchargeKeys.Count > 0)
        {
            await DispatchSurchargeSettingsChangedAsync(changedSurchargeKeys);
        }

        return resource;
    }

    private async Task RestoreSurchargeSnapshotAsync(CompanyRule rule, List<string> changedSurchargeKeys)
    {
        changedSurchargeKeys.Clear();

        if (string.IsNullOrWhiteSpace(rule.SettingsSnapshotJson))
        {
            return;
        }

        var snapshot = JsonSerializer.Deserialize<Dictionary<string, string?>>(rule.SettingsSnapshotJson)
                       ?? new Dictionary<string, string?>();

        foreach (var (settingKey, previousValue) in snapshot)
        {
            if (previousValue is null)
            {
                continue;
            }

            if (!CompanyRuleSurchargeSettingMap.ParameterToSettingKey.Values.Contains(settingKey))
            {
                _logger.LogWarning("Revert: snapshot key {SettingKey} is not a known surcharge setting; skipped.", settingKey);
                continue;
            }

            var current = await _settingsRepository.GetSetting(settingKey);
            var currentValue = current?.Value;
            await _settingsRepository.UpsertSettingAsync(settingKey, previousValue);

            if (SurchargeRelevantSettingKeys.All.Contains(settingKey)
                && !string.Equals(currentValue, previousValue, StringComparison.Ordinal))
            {
                changedSurchargeKeys.Add(settingKey);
            }
        }
    }

    private async Task DispatchSurchargeSettingsChangedAsync(IReadOnlyCollection<string> changedKeys)
    {
        try
        {
            await _eventDispatcher.DispatchAsync(new SurchargeSettingsChangedEvent(changedKeys), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed after reverting a company rule ({Keys}); the revert is persisted and remains unaffected.",
                nameof(SurchargeSettingsChangedEvent),
                string.Join(", ", changedKeys));
        }
    }

    private async Task RevertCounterRuleAsync(CompanyRule rule, CancellationToken cancellationToken)
    {
        if (!rule.TargetEntityId.HasValue)
        {
            return;
        }

        try
        {
            await _mediator.Send(new Commands.DeleteCommand<CounterRuleResource>(rule.TargetEntityId.Value), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogInformation("Revert: counter rule {Id} was already gone; tolerated.", rule.TargetEntityId);
        }
    }

    private async Task RevertCustomMacroAsync(CompanyRule rule, CancellationToken cancellationToken)
    {
        if (!rule.TargetEntityId.HasValue)
        {
            return;
        }

        try
        {
            await _mediator.Send(new MacroCommands.DeleteCommand(rule.TargetEntityId.Value), cancellationToken);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            _logger.LogInformation("Revert: macro {Id} was already gone; tolerated.", rule.TargetEntityId);
        }
    }

}
