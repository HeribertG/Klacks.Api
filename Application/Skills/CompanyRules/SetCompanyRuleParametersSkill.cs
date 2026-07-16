// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets one or more parameters on the in-progress company-rule draft. Each value is validated
/// individually against the catalog definition; invalid values are reported and NOT stored, valid ones
/// are merged into the draft. Returns the per-parameter outcome and the parameters still missing.
/// Draft-only; nothing is persisted to the domain.
/// </summary>
/// <param name="parameters">Required. A JSON object of parameterName to value.</param>

using System.Text.Json;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.CompanyRules;

[SkillImplementation("set_company_rule_parameters")]
public class SetCompanyRuleParametersSkill : BaseSkillImplementation
{
    private const string ParametersKey = "parameters";

    private readonly IPendingCompanyRuleDraftStore _draftStore;
    private readonly ICompanyRuleParameterCatalog _catalog;
    private readonly ICompanyRuleDraftValidator _validator;

    public SetCompanyRuleParametersSkill(
        IPendingCompanyRuleDraftStore draftStore,
        ICompanyRuleParameterCatalog catalog,
        ICompanyRuleDraftValidator validator)
    {
        _draftStore = draftStore;
        _catalog = catalog;
        _validator = validator;
    }

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var conversationKey = CompanyRuleDraftScope.ConversationKey(context);
        var draft = _draftStore.Get(context.UserId, conversationKey);
        if (draft is null)
        {
            return Task.FromResult(SkillResult.Error(
                "No company rule draft in progress. Start one with start_company_rule first."));
        }

        if (!TryReadParameterMap(parameters, out var values, out var mapError))
        {
            return Task.FromResult(SkillResult.Error(mapError!));
        }

        if (values.Count == 0)
        {
            return Task.FromResult(SkillResult.Error(
                "No parameters provided. Pass a JSON object of parameterName to value."));
        }

        var accepted = new List<string>();
        var rejected = new Dictionary<string, string>();

        foreach (var (name, value) in values)
        {
            var validation = _validator.Validate(draft.Kind, name, value);
            if (validation.IsValid)
            {
                draft.Parameters[name] = value;
                accepted.Add(name);
            }
            else
            {
                rejected[name] = validation.ErrorMessage!;
            }
        }

        _draftStore.Set(context.UserId, conversationKey, draft);

        var checklist = CompanyRuleChecklist.Build(draft, _catalog, _validator);
        var data = new
        {
            Accepted = accepted,
            Rejected = rejected,
            Checklist = checklist
        };

        var message = rejected.Count == 0
            ? $"Stored {accepted.Count} parameter(s). Check the remaining open parameters before previewing."
            : $"Stored {accepted.Count} parameter(s); rejected {rejected.Count}. The rejected values were not saved — fix them and try again.";

        return Task.FromResult(SkillResult.SuccessResult(data, message));
    }

    private static bool TryReadParameterMap(
        Dictionary<string, object> parameters, out Dictionary<string, string> values, out string? error)
    {
        values = new Dictionary<string, string>(StringComparer.Ordinal);
        error = null;

        if (!parameters.TryGetValue(ParametersKey, out var raw) || raw is null)
        {
            error = "The 'parameters' object is required.";
            return false;
        }

        try
        {
            var json = raw is JsonElement element
                ? element.GetRawText()
                : raw as string ?? JsonSerializer.Serialize(raw);

            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "The 'parameters' value must be a JSON object of parameterName to value.";
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                values[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.GetRawText();
            }

            return true;
        }
        catch (JsonException)
        {
            error = "The 'parameters' value could not be parsed as a JSON object.";
            return false;
        }
    }
}
