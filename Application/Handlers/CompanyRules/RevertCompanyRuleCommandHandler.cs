// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reverts an applied company rule. SurchargeSettings writes the snapshotted setting values back (keys
/// that did not exist before the apply cannot be removed because settings have no delete path, and are
/// left untouched); a snapshot key outside <see cref="CompanyRuleSurchargeSettingMap"/> is skipped and
/// logged rather than written, as defense-in-depth against a tampered or corrupted snapshot. CounterRule
/// and CustomMacro soft-delete the target entity they created, tolerating a target that is already gone.
/// In every case the registry row is soft-deleted. A genuine delete blocker (for example a macro still
/// referenced by shifts) is surfaced as its real message.
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

    public RevertCompanyRuleCommandHandler(
        ICompanyRuleRepository companyRuleRepository,
        ISettingsRepository settingsRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ILogger<RevertCompanyRuleCommandHandler> logger,
        SettingsMapper mapper)
    {
        _companyRuleRepository = companyRuleRepository;
        _settingsRepository = settingsRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<CompanyRuleResource?> Handle(RevertCompanyRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _companyRuleRepository.GetAsync(request.Id);
        if (rule is null)
        {
            throw new InvalidRequestException($"No applied company rule found with id '{request.Id}'.");
        }

        var resource = _mapper.ToCompanyRuleResource(rule);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            switch (rule.Kind)
            {
                case CompanyRuleKind.SurchargeSettings:
                    await RestoreSurchargeSnapshotAsync(rule);
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

        return resource;
    }

    private async Task RestoreSurchargeSnapshotAsync(CompanyRule rule)
    {
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

            await _settingsRepository.UpsertSettingAsync(settingKey, previousValue);
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
