// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// K20 entity import of Macro rows from the top-level macros section: compiles and probe-executes every
/// script, plans insert/update/skip against the stored rows plus the demotion of the current holder of
/// each imported (category, function) slot, and applies demotions and decisions inside the import
/// transaction. Keep the hashed fields (ComputeContentHash), the applied fields (ApplyDecisions) and
/// MacroImportValues in sync.
/// </summary>
/// <param name="macroImportRepository">Reads and stages imported Macro rows and finds function holders</param>
/// <param name="macroScriptValidator">Compiles and probe-executes imported macro scripts inside a hard timeout</param>
/// <param name="unitOfWork">Flushes the demotions before the new holders are inserted</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class MacroImporter
{
    private const string SourceKeyPrefix = "region-setup:macros";

    private readonly IMacroImportRepository _macroImportRepository;
    private readonly IMacroScriptValidator _macroScriptValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MacroImporter> _logger;

    public MacroImporter(
        IMacroImportRepository macroImportRepository,
        IMacroScriptValidator macroScriptValidator,
        IUnitOfWork unitOfWork,
        ILogger<MacroImporter> logger)
    {
        _macroImportRepository = macroImportRepository;
        _macroScriptValidator = macroScriptValidator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public List<EntityImportDesired<MacroImportValues>> BuildDesired(List<RegionSetupMacro>? macros)
    {
        if (macros == null || macros.Count == 0)
        {
            return [];
        }

        var seenKeys = new HashSet<string>();
        var seenFunctions = new HashSet<(MacroCategoryEnum Category, MacroFunctionEnum Function)>();
        var desired = new List<EntityImportDesired<MacroImportValues>>();

        foreach (var macro in macros)
        {
            if (string.IsNullOrWhiteSpace(macro.Name) || string.IsNullOrWhiteSpace(macro.Content))
            {
                throw new InvalidRequestException("Region setup: macros entry requires a non-empty 'name' and 'content'.");
            }

            ValidateMacroScript(macro.Name!, macro.Content!);

            var function = ParseMacroFunction(macro.Function, $"macros ('{macro.Name}').function");
            var category = ParseMacroCategory(macro.Category, $"macros ('{macro.Name}').category");

            if (function != MacroFunctionEnum.Custom && !seenFunctions.Add((category, function)))
            {
                throw new InvalidRequestException(
                    $"Region setup: macros contains more than one entry with function '{function}' for category '{category}'.");
            }

            var name = macro.Name!.Trim();
            var sourceKey = $"{SourceKeyPrefix}:{RegionSetupFieldParser.BuildImportSlug(name)}";
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: macros contains more than one entry resolving to key '{sourceKey}'.");
            }

            var values = new MacroImportValues(name, macro.Content!, category, function);
            desired.Add(new EntityImportDesired<MacroImportValues>(
                sourceKey,
                ComputeContentHash(values.Name, values.Content, values.Category, values.Function),
                values));
        }

        return desired;
    }

    // Importing a non-custom function may displace the CURRENT holder of that (category, function) slot,
    // but only when that holder is one of the seeded standard macros or itself an unedited import - a
    // customer-created (or customer-edited) holder is never demoted silently; the import fails with a
    // message telling the operator to resolve the conflict deliberately. Demotions are planned here
    // (fail-fast before any write) and executed first inside the transaction, so the partial unique
    // index (category, type) never sees two active holders.
    public async Task<MacroImportPlan> PlanAsync(IReadOnlyList<EntityImportDesired<MacroImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return MacroImportPlan.Empty;
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _macroImportRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(m => m.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            m => m.ImportSourceKey,
            m => ComputeContentHash(m.Name, m.Content, m.Category, (MacroFunctionEnum)m.Type) == m.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);

        var demotions = new List<Macro>();
        foreach (var decision in decisions)
        {
            if (decision.Action == EntityImportAction.SkipEdited || decision.Values.Function == MacroFunctionEnum.Custom)
            {
                continue;
            }

            var holder = await _macroImportRepository.GetActiveFunctionHolderAsync(decision.Values.Category, decision.Values.Function);
            if (holder == null || holder.ImportSourceKey == decision.SourceKey)
            {
                continue;
            }

            var isSeeded = holder.Id == SeededMacroIds.AllShift || holder.Id == SeededMacroIds.AllShiftAdditive;
            var isUneditedImport = holder.ImportSourceKey.Length > 0
                && ComputeContentHash(holder.Name, holder.Content, holder.Category, (MacroFunctionEnum)holder.Type) == holder.ImportContentHash;
            if (!isSeeded && !isUneditedImport)
            {
                throw new InvalidRequestException(
                    $"Region setup: macro '{decision.Values.Name}' wants function '{decision.Values.Function}' for category '{decision.Values.Category}', but customer macro '{holder.Name}' currently carries it. Set that macro's function to Custom first, then re-run the setup.");
            }

            demotions.Add(holder);
        }

        return new MacroImportPlan(new EntityImportFamilyPlan<MacroImportValues, Macro>(decisions, existingBySourceKey), demotions);
    }

    // Flushed via CompleteAsync BEFORE the new function holders are inserted: relying on EF Core's
    // internal statement ordering against the partial unique index (category, type) is fragile
    // (dotnet/efcore#28065/#30601 document exactly this filtered-index constellation), and a violation
    // here would crash the whole application start. Both flushes run inside the same outer transaction,
    // so atomicity is preserved.
    public async Task ApplyDemotionsAsync(IReadOnlyList<Macro> demotions)
    {
        if (demotions.Count == 0)
        {
            return;
        }

        foreach (var demoted in demotions)
        {
            demoted.Type = (int)MacroFunctionEnum.Custom;
            _macroImportRepository.Update(demoted);
            _logger.LogInformation(
                "Region setup: macro '{Name}' demoted to Custom - the imported profile ships its own holder for that function",
                demoted.Name);
        }

        await _unitOfWork.CompleteAsync();
    }

    public void ApplyDecisions(EntityImportFamilyPlan<MacroImportValues, Macro> plan)
    {
        foreach (var decision in plan.Decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _macroImportRepository.Add(new Macro
                    {
                        Id = Guid.NewGuid(),
                        Name = decision.Values.Name,
                        Content = decision.Values.Content,
                        Category = decision.Values.Category,
                        Type = (int)decision.Values.Function,
                        Description = new MultiLanguage(),
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = plan.ExistingBySourceKey[decision.SourceKey];
                    existing.Name = decision.Values.Name;
                    existing.Content = decision.Values.Content;
                    existing.Category = decision.Values.Category;
                    existing.Type = (int)decision.Values.Function;
                    existing.ImportContentHash = decision.ContentHash;
                    _macroImportRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: macro '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    private static string ComputeContentHash(string name, string content, MacroCategoryEnum category, MacroFunctionEnum function)
    {
        return ImportContentHasher.ComputeHash(name, content, category.ToString(), function.ToString());
    }

    // Delegates to the shared IMacroScriptValidator (compile + probe-execute inside a hard timeout,
    // because static compilation alone misses parser hangs and most malformed input) and converts a
    // validation failure into a fail-fast import error.
    private void ValidateMacroScript(string name, string content)
    {
        var validation = _macroScriptValidator.Validate(content);
        if (!validation.IsValid)
        {
            throw new InvalidRequestException(
                $"Region setup: macros entry '{name}' failed validation: {validation.ErrorMessage}");
        }
    }

    private static MacroFunctionEnum ParseMacroFunction(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MacroFunctionEnum.Custom;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "custom" => MacroFunctionEnum.Custom,
            "standard" => MacroFunctionEnum.Standard,
            "standardadditive" => MacroFunctionEnum.StandardAdditive,
            _ => throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be 'custom', 'standard' or 'standardAdditive'."),
        };
    }

    private static MacroCategoryEnum ParseMacroCategory(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MacroCategoryEnum.Shift;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "shift" => MacroCategoryEnum.Shift,
            "vacation" => MacroCategoryEnum.Vacation,
            "illness" => MacroCategoryEnum.Illness,
            "accident" => MacroCategoryEnum.Accident,
            "workshoppaid" => MacroCategoryEnum.WorkshopPaid,
            "workshopunpaid" => MacroCategoryEnum.WorkshopUnpaid,
            _ => throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be 'shift', 'vacation', 'illness', 'accident', 'workshopPaid' or 'workshopUnpaid'."),
        };
    }
}
