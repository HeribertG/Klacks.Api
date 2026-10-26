// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that searches for employees or customers by criteria such as name, canton, or entity type
/// and returns a filtered list of matches with basic information.
/// To search AND directly navigate to a specific person, use search_and_navigate instead.
/// A search term is looked up with the text search and, when that finds nobody, with the fuzzy (trigram and
/// phonetic) search. Recipes capture the lone row of Results and inject its id into a writing step, so Results
/// only holds the people the term names under the Strict name policy (ClientNameMatch: equal words or a forward match
/// on whole words, first and last name in either order, company, client number). Every other row - a hit inside a
/// word ("Meierhans" for "Meier") or only by similarity ("Mayer" for "Meier") - goes to OtherMatches without an id;
/// when no row is picked the message is the name ask-back, the capture finds no row and the recipe rewinds to its
/// question. A term is judged over the whole text-search result (up to 100 rows, re-queried when the first page is
/// incomplete); when even that is incomplete nobody is picked, because a lone match on the page need not be the only
/// person the term names. Results never shrink to a single row while more people match.
/// </summary>
/// <param name="searchTerm">Optional name, company or client number</param>
/// <param name="canton">Optional state of any of the person's addresses - canton, Bundesland, département, ... - as
/// code or as name in any installed language; resolved to the stored code (IAddressLocationResolver)</param>
/// <param name="entityType">Optional Employee, ExternEmp or Customer</param>
/// <param name="limit">Optional maximum number of rows (default 10)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("search_employees")]
public class SearchEmployeesSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 10;
    private const int JudgedRowLimit = 100;
    private const int SmallestAmbiguousRowCount = 2;

    private readonly IClientSearchRepository _searchRepository;
    private readonly IAddressLocationResolver _locationResolver;

    public SearchEmployeesSkill(IClientSearchRepository searchRepository, IAddressLocationResolver locationResolver)
    {
        _searchRepository = searchRepository;
        _locationResolver = locationResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var canton = await _locationResolver.ResolveStateCodeAsync(
            GetParameter<string>(parameters, "canton"), cancellationToken);
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
        else if (result.TotalCount > result.Items.Count)
        {
            result = await _searchRepository.SearchAsync(
                searchTerm, canton, entityType, null, JudgedRowLimit, cancellationToken);
        }

        if (result.TotalCount > result.Items.Count)
        {
            return IncompleteResult(searchTerm, result.Items.Count, result.Items.Take(limit).ToList());
        }

        var partition = ClientNameMatch.Partition(result.Items, searchTerm);
        if (partition.Matches.Count == 0 && partition.Suggestions.Count > 0)
        {
            return SuggestionResult(searchTerm, partition.Suggestions.Take(limit).ToList());
        }

        var shownCount = partition.Matches.Count > 1 ? Math.Max(limit, SmallestAmbiguousRowCount) : limit;
        var shown = partition.Matches.Take(shownCount).ToList();
        return ListResult(
            shown, partition.Matches.Count, partition.Matches.Count > shown.Count, limit, searchTerm, canton,
            partition.Suggestions.Take(limit).ToList());
    }

    private static SkillResult IncompleteResult(
        string searchTerm, int judgedCount, IReadOnlyList<ClientSearchItem> sample)
    {
        var resultData = new
        {
            Results = Array.Empty<ClientSearchItem>(),
            Count = 0,
            TotalCount = 0,
            HasMore = true,
            OtherMatches = sample.Select(ToSuggestion).ToList()
        };

        return SkillResult.SuccessResult(
            resultData,
            $"More than {judgedCount} people match '{searchTerm}', so the list is incomplete and nobody was picked. " +
            "Ask the user for the full first and last name or the client number; a sample is listed under " +
            "OtherMatches without an id. " + NameResolutionAskBack.Marker);
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
              (hasMore ? $". Showing first {items.Count}." : ".");

        if (suggestionRows.Count > 0)
        {
            message += $" {suggestionRows.Count} further person(s) only contain the term inside a word or have a " +
                       "similar name; they are listed under OtherMatches without an id; search again with the full " +
                       "name if the user means one of them.";
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
            ? NameResolutionMessages.SuggestedName(ClientResolver.DescribeOne(suggestions[0]))
            : NameResolutionMessages.SuggestedNames(
                searchTerm, string.Join(", ", suggestions.Select(ClientResolver.DescribeOne)));

        return SkillResult.SuccessResult(resultData, message);
    }

    private static object ToSuggestion(ClientSearchItem item) =>
        new { item.IdNumber, item.FirstName, item.LastName, item.Company, item.EntityType };
}
