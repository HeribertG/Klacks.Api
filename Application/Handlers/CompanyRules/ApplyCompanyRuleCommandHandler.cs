// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the pending company-rule draft. SurchargeSettings snapshots the overwritten setting values
/// and writes the new ones; CounterRule resolves the optional scheduling-rule scope by name, maps the
/// warn/block enforcement choice onto the rule's per-rule override and creates a counter rule; CustomMacro
/// rejects a name collision and creates a custom macro. In every case a
/// registry row is written and the draft is cleared. Domain problems (missing parameters, ambiguous or
/// unknown scheduling rule, macro name collision, invalid macro script) surface as
/// <see cref="InvalidRequestException"/> so the calling skill can relay the real message.
/// </summary>
/// <param name="request">Identifies the pending draft by user and conversation key.</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Application.Commands.CompanyRules;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Mediator;
using MacroCommands = Klacks.Api.Application.Commands.Settings.Macros;
using MacroQueries = Klacks.Api.Application.Queries.Settings.Macros;

namespace Klacks.Api.Application.Handlers.CompanyRules;

public class ApplyCompanyRuleCommandHandler : IRequestHandler<ApplyCompanyRuleCommand, CompanyRuleResource?>
{
    private const string DefaultSurchargeRuleName = "Surcharge settings";

    private readonly IPendingCompanyRuleDraftStore _draftStore;
    private readonly ICompanyRuleDraftValidator _validator;
    private readonly ICompanyRuleParameterCatalog _catalog;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICompanyRuleRepository _companyRuleRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SettingsMapper _mapper;

    public ApplyCompanyRuleCommandHandler(
        IPendingCompanyRuleDraftStore draftStore,
        ICompanyRuleDraftValidator validator,
        ICompanyRuleParameterCatalog catalog,
        ISettingsRepository settingsRepository,
        ICompanyRuleRepository companyRuleRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        SettingsMapper mapper)
    {
        _draftStore = draftStore;
        _validator = validator;
        _catalog = catalog;
        _settingsRepository = settingsRepository;
        _companyRuleRepository = companyRuleRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CompanyRuleResource?> Handle(ApplyCompanyRuleCommand request, CancellationToken cancellationToken)
    {
        var draft = _draftStore.Get(request.UserId, request.ConversationKey);
        if (draft is null)
        {
            throw new InvalidRequestException(
                "No company rule draft in progress. Start one with start_company_rule before applying.");
        }

        var missing = _validator.GetMissingRequired(draft);
        if (missing.Count > 0)
        {
            throw new InvalidRequestException(
                $"Cannot apply the company rule: still missing required parameter(s): {string.Join(", ", missing)}.");
        }

        var entity = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var (targetType, targetId, snapshotJson) = draft.Kind switch
            {
                CompanyRuleKind.SurchargeSettings => await ApplySurchargeAsync(draft),
                CompanyRuleKind.CounterRule => await ApplyCounterRuleAsync(draft, cancellationToken),
                CompanyRuleKind.CustomMacro => await ApplyCustomMacroAsync(draft, cancellationToken),
                _ => throw new InvalidRequestException($"Unsupported company rule kind '{draft.Kind}'.")
            };

            var registryEntity = new CompanyRule
            {
                Id = Guid.NewGuid(),
                Name = ResolveRegistryName(draft),
                RuleText = draft.RuleText,
                Kind = draft.Kind,
                TargetEntityType = targetType,
                TargetEntityId = targetId,
                SettingsSnapshotJson = snapshotJson,
                AppliedParametersJson = JsonSerializer.Serialize(draft.Parameters)
            };

            _companyRuleRepository.Add(registryEntity);
            await _unitOfWork.CompleteAsync();
            return registryEntity;
        });

        _draftStore.Clear(request.UserId, request.ConversationKey);
        return _mapper.ToCompanyRuleResource(entity);
    }

    private async Task<(string TargetType, Guid? TargetId, string? SnapshotJson)> ApplySurchargeAsync(CompanyRuleDraft draft)
    {
        var snapshot = new Dictionary<string, string?>();

        foreach (var (parameterName, settingKey) in CompanyRuleSurchargeSettingMap.ParameterToSettingKey)
        {
            if (!draft.Parameters.TryGetValue(parameterName, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var existing = await _settingsRepository.GetSetting(settingKey);
            snapshot[settingKey] = existing?.Value;

            await _settingsRepository.UpsertSettingAsync(settingKey, CanonicalValue(CompanyRuleKind.SurchargeSettings, parameterName, raw));
        }

        return (CompanyRuleTargetEntityTypes.Settings, null, JsonSerializer.Serialize(snapshot));
    }

    private async Task<(string TargetType, Guid? TargetId, string? SnapshotJson)> ApplyCounterRuleAsync(
        CompanyRuleDraft draft, CancellationToken cancellationToken)
    {
        var eventType = Enum.Parse<CounterEventType>(draft.Parameters[CompanyRuleParameterNames.EventType], ignoreCase: true);
        var period = Enum.Parse<CounterPeriod>(draft.Parameters[CompanyRuleParameterNames.Period], ignoreCase: true);
        var threshold = int.Parse(draft.Parameters[CompanyRuleParameterNames.Threshold], CultureInfo.InvariantCulture);

        decimal? hoursThreshold = null;
        if (draft.Parameters.TryGetValue(CompanyRuleParameterNames.HoursThreshold, out var hoursRaw) && !string.IsNullOrWhiteSpace(hoursRaw))
        {
            hoursThreshold = decimal.Parse(hoursRaw, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        Guid? schedulingRuleId = null;
        if (draft.Parameters.TryGetValue(CompanyRuleParameterNames.SchedulingRuleName, out var ruleName) && !string.IsNullOrWhiteSpace(ruleName))
        {
            var rules = (await _mediator.Send(new Queries.ListQuery<SchedulingRuleResource>(), cancellationToken) ?? []).ToList();
            var (rule, error) = SchedulingRuleResolver.Resolve(rules, ruleName);
            if (rule is null)
            {
                throw new InvalidRequestException(error!);
            }

            schedulingRuleId = rule.Id;
        }

        var enforcement = Enum.Parse<RuleEnforcementMode>(draft.Parameters[CompanyRuleParameterNames.Enforcement], ignoreCase: true);

        var resource = new CounterRuleResource
        {
            EventType = eventType,
            Period = period,
            Threshold = threshold,
            HoursThreshold = hoursThreshold,
            Enforcement = enforcement,
            SchedulingRuleId = schedulingRuleId
        };

        var created = await _mediator.Send(new Commands.PostCommand<CounterRuleResource>(resource), cancellationToken);
        if (created is null)
        {
            throw new InvalidRequestException("The counter rule could not be created.");
        }

        return (CompanyRuleTargetEntityTypes.CounterRule, created.Id, null);
    }

    private async Task<(string TargetType, Guid? TargetId, string? SnapshotJson)> ApplyCustomMacroAsync(
        CompanyRuleDraft draft, CancellationToken cancellationToken)
    {
        var name = draft.Parameters[CompanyRuleParameterNames.MacroName].Trim();
        var script = draft.Parameters[CompanyRuleParameterNames.MacroScript];
        draft.Parameters.TryGetValue(CompanyRuleParameterNames.MacroDescription, out var description);

        var category = MacroCategoryEnum.Shift;
        if (draft.Parameters.TryGetValue(CompanyRuleParameterNames.MacroCategory, out var categoryRaw) && !string.IsNullOrWhiteSpace(categoryRaw))
        {
            category = Enum.Parse<MacroCategoryEnum>(categoryRaw, ignoreCase: true);
        }

        var existingMacros = await _mediator.Send(new MacroQueries.ListQuery(), cancellationToken);
        if (existingMacros.Any(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidRequestException(
                $"A macro named '{name}' already exists. This flow only creates new macros — overwriting an " +
                "existing macro must be done explicitly via the macro update path (update_macro), not the " +
                "company-rule apply flow.");
        }

        var resource = new MacroResource
        {
            Name = name,
            Content = script,
            Type = (int)MacroFunctionEnum.Custom,
            Category = category,
            Description = BuildDescription(description)
        };

        var created = await _mediator.Send(new MacroCommands.PostCommand(resource), cancellationToken);
        if (created is null)
        {
            throw new InvalidRequestException($"Macro '{name}' could not be created.");
        }

        return (CompanyRuleTargetEntityTypes.Macro, created.Id, null);
    }

    private string ResolveRegistryName(CompanyRuleDraft draft)
    {
        if (!string.IsNullOrWhiteSpace(draft.Name))
        {
            return draft.Name.Trim();
        }

        return draft.Kind switch
        {
            CompanyRuleKind.CustomMacro => draft.Parameters[CompanyRuleParameterNames.MacroName].Trim(),
            CompanyRuleKind.CounterRule => ResolveCounterRuleName(draft),
            _ => DefaultSurchargeRuleName
        };
    }

    private static string ResolveCounterRuleName(CompanyRuleDraft draft)
    {
        if (draft.Parameters.TryGetValue(CompanyRuleParameterNames.CounterRuleName, out var explicitName) && !string.IsNullOrWhiteSpace(explicitName))
        {
            return explicitName.Trim();
        }

        var eventType = draft.Parameters[CompanyRuleParameterNames.EventType];
        var threshold = draft.Parameters[CompanyRuleParameterNames.Threshold];
        var period = draft.Parameters[CompanyRuleParameterNames.Period];
        return $"{eventType} >= {threshold} per {period}";
    }

    private string CanonicalValue(CompanyRuleKind kind, string parameterName, string rawValue)
    {
        var definition = _catalog.FindParameter(kind, parameterName);
        if (definition is { DataType: CompanyRuleParameterDataType.Enum, EnumValues: { } allowed })
        {
            foreach (var candidate in allowed)
            {
                if (string.Equals(candidate, rawValue, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
        }

        return rawValue.Trim();
    }

    private static MultiLanguage BuildDescription(string? description)
    {
        var value = new MultiLanguage();
        if (string.IsNullOrWhiteSpace(description))
        {
            return value;
        }

        foreach (var language in MultiLanguage.CoreLanguages)
        {
            value.SetValue(language, description);
        }

        return value;
    }
}
