// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bulk-imports calendar rules (holidays definitions) for a country + state. Each entry in
/// the rules JSON array becomes one CalendarRule row. Validates the rule grammar via
/// HolidaysListCalculator before persisting; rejects the whole batch if any rule is invalid.
/// </summary>
/// <param name="country">Required. ISO country code attached to every rule in this batch.</param>
/// <param name="state">Optional. State / canton attached to every rule; empty for nationwide.</param>
/// <param name="rulesJson">Required. JSON array of objects { rule, subRule?, nameDe, nameEn?, isMandatory?, isPaid? }.</param>

using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("import_calendar_rules")]
public class ImportCalendarRulesSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportCalendarRulesSkill(ISettingsRepository settingsRepository, IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var country = GetRequiredString(parameters, "country").Trim().ToUpperInvariant();
        var state = (GetParameter<string>(parameters, "state") ?? string.Empty).Trim().ToUpperInvariant();
        var rulesJson = GetRequiredString(parameters, "rulesJson");

        List<RuleInput> inputs;
        try
        {
            inputs = JsonSerializer.Deserialize<List<RuleInput>>(rulesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch (JsonException ex)
        {
            return SkillResult.Error($"rulesJson is not a valid JSON array: {ex.Message}");
        }

        if (inputs.Count == 0)
        {
            return SkillResult.Error("rulesJson contained no rules.");
        }

        var year = DateTime.UtcNow.Year;
        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            if (string.IsNullOrWhiteSpace(input.Rule))
            {
                return SkillResult.Error($"Rule #{index + 1} has empty 'rule' field.");
            }
            if (string.IsNullOrWhiteSpace(input.NameDe))
            {
                return SkillResult.Error($"Rule #{index + 1} has empty 'nameDe' field.");
            }

            var calculator = new HolidaysListCalculator { CurrentYear = year };
            try
            {
                calculator.Add(new CalendarRule
                {
                    Rule = input.Rule,
                    SubRule = input.SubRule ?? string.Empty,
                    IsMandatory = true
                });
                calculator.ComputeHolidays();
            }
            catch (Exception ex)
            {
                return SkillResult.Error($"Rule #{index + 1} '{input.Rule}' is invalid: {ex.Message}");
            }

            if (calculator.HolidayList.Count == 0)
            {
                return SkillResult.Error($"Rule #{index + 1} '{input.Rule}' did not produce a valid date for {year}.");
            }
        }

        var created = new List<Guid>();
        foreach (var input in inputs)
        {
            var nameMl = new MultiLanguage();
            nameMl.SetValue("de", input.NameDe);
            if (!string.IsNullOrWhiteSpace(input.NameEn))
            {
                nameMl.SetValue("en", input.NameEn);
            }

            var rule = new CalendarRule
            {
                Id = Guid.NewGuid(),
                Country = country,
                State = state,
                Rule = input.Rule!,
                SubRule = input.SubRule ?? string.Empty,
                Name = nameMl,
                Description = MultiLanguage.Empty(),
                IsMandatory = input.IsMandatory ?? true,
                IsPaid = input.IsPaid ?? true
            };

            _settingsRepository.AddCalendarRule(rule);
            created.Add(rule.Id);
        }

        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                Country = country,
                State = state,
                ImportedCount = created.Count,
                ImportedIds = created
            },
            $"Imported {created.Count} calendar rule(s) for {country}/{(string.IsNullOrEmpty(state) ? "—" : state)}.");
    }

    private sealed class RuleInput
    {
        public string? Rule { get; set; }
        public string? SubRule { get; set; }
        public string? NameDe { get; set; }
        public string? NameEn { get; set; }
        public bool? IsMandatory { get; set; }
        public bool? IsPaid { get; set; }
    }
}
