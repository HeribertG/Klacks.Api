// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that searches for employees or customers by criteria such as name, canton, or entity type
/// and returns a filtered list of matches with basic information.
/// To search AND directly navigate to a specific person, use search_and_navigate instead.
/// A search term is looked up with the text search and, when that finds nobody, with the fuzzy (trigram and
/// phonetic) search. Recipes capture the lone row of Results and inject its id into a writing step, so Results
/// only holds the people the Strict name policy picks for the term (ClientNameMatch: equal words first, otherwise a
/// forward match on whole words, in either name order). Every other row - one that matches less exactly, inside a
/// word ("Meierhans" for "Meier") or only by similarity ("Mayer" for "Meier") - goes to OtherMatches without an id;
/// when no row is picked the message is the name ask-back, the capture finds no row and the recipe rewinds to its
/// question.
/// </summary>
/// <param name="searchTerm">Optional name, company or client number</param>
/// <param name="canton">Optional canton/state code of the employee address</param>
/// <param name="entityType">Optional Employee, ExternEmp or Customer</param>
/// <param name="limit">Optional maximum number of rows (default 10)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("search_employees")]
public class SearchEmployeesSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 10;

    private readonly IClientSearchRepository _searchRepository;

    public SearchEmployeesSkill(IClientSearchRepository searchRepository)
    {
        _searchRepository = searchRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var canton = GetParameter<string>(parameters, "canton");
        var entityTypeValue = GetParameter<string>(parameters, "entityType");
        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;

        EntityTypeEnum? entityType = entityTypeValue switch
        {
            "Employee" => EntityTypeEnum.Employee,
            "ExternEmp" => EntityTypeEnum.ExternEmp,
            "Customer" => EntityTypeEnum.Customer,
            _ => null
        };

        var result = await _searchRepository.SearchAsync(
            searchTerm,
            canton,
            entityType,
            null,
            limit,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return ListResult(result.Items, result.TotalCount, result.TotalCount > limit, limit, searchTerm, canton);
        }

        if (result.Items.Count == 0)
        {
            result = await _searchRepository.SearchByFuzzyNameAsync(
                searchTerm, canton, entityType, null, limit, cancellationToken);
        }

        var partition = ClientNameMatch.Partition(result.Items, searchTerm);
        var matchIds = partition.Matches.Select(item => item.Id).ToHashSet();
        var suggestions = result.Items.Where(item => !matchIds.Contains(item.Id)).ToList();

        if (partition.Matches.Count == 0 && suggestions.Count > 0)
        {
            return SuggestionResult(searchTerm, suggestions);
        }

        var matches = result.Items.Where(item => matchIds.Contains(item.Id)).ToList();
        var totalCount = suggestions.Count == 0 ? result.TotalCount : matches.Count;
        return ListResult(
            matches, totalCount, result.TotalCount > result.Items.Count, limit, searchTerm, canton, suggestions);
    }

    private static SkillResult ListResult(
        IReadOnlyList<ClientSearchItem> items,
        int totalCount,
        bool hasMore,
        int limit,
        string? searchTerm,
        string? canton,
        IReadOnlyList<ClientSearchItem>? suggestions = null)
    {
        var suggestionRows = (suggestions ?? []).Select(ToSuggestion).ToList();
        var resultData = new
        {
            Results = items,
            Count = items.Count,
            TotalCount = totalCount,
            HasMore = hasMore,
            OtherMatches = suggestionRows
        };

        var message = totalCount == 0
            ? "No employees found matching the criteria."
            : $"Found {totalCount} employee(s)" +
              (!string.IsNullOrWhiteSpace(searchTerm) ? $" matching '{searchTerm}'" : "") +
              (!string.IsNullOrWhiteSpace(canton) ? $" in canton {canton}" : "") +
              (hasMore ? $". Showing first {limit}." : ".");

        if (suggestionRows.Count > 0)
        {
            message += $" {suggestionRows.Count} further person(s) match the term less exactly and are listed under " +
                       "OtherMatches without an id; search again with the full name if the user means one of them.";
        }

        return SkillResult.SuccessResult(resultData, message);
    }

    private static SkillResult SuggestionResult(string searchTerm, IReadOnlyList<ClientSearchItem> suggestions)
    {
        var resultData = new
        {
            Results = Array.Empty<ClientSearchItem>(),
            Count = 0,
            TotalCount = 0,
            HasMore = false,
            OtherMatches = suggestions.Select(ToSuggestion).ToList()
        };

        var message = suggestions.Count == 1
            ? NameResolutionMessages.SuggestedName(Describe(suggestions[0]))
            : NameResolutionMessages.SuggestedNames(searchTerm, string.Join(", ", suggestions.Select(Describe)));

        return SkillResult.SuccessResult(resultData, message);
    }

    private static object ToSuggestion(ClientSearchItem item) =>
        new { item.IdNumber, item.FirstName, item.LastName, item.Company, item.EntityType };

    private static string Describe(ClientSearchItem item)
    {
        var name = $"{item.FirstName} {item.LastName}".Trim();
        if (string.IsNullOrEmpty(name))
        {
            name = item.Company ?? string.Empty;
        }

        return $"{name} (#{item.IdNumber})";
    }
}
