// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_page_controls")]
public class GetPageControlsSkill : BaseSkillImplementation
{
    private readonly IUiControlRepository _uiControlRepository;

    public GetPageControlsSkill(IUiControlRepository uiControlRepository)
    {
        _uiControlRepository = uiControlRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var page = GetRequiredString(parameters, "page").Trim().ToLowerInvariant();

        if (page == UiPageKeys.ListCommand)
        {
            return await GetPageListAsync(cancellationToken);
        }

        return await GetControlsForPageAsync(page, cancellationToken);
    }

    private async Task<SkillResult> GetPageListAsync(CancellationToken cancellationToken)
    {
        var pageKeys = await _uiControlRepository.GetDistinctPageKeysAsync(cancellationToken);

        if (pageKeys.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { Pages = Array.Empty<string>() },
                "No UI pages registered yet.");
        }

        var resultData = new
        {
            Pages = pageKeys,
            TotalPages = pageKeys.Count
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Found {pageKeys.Count} registered page(s): {string.Join(", ", pageKeys)}");
    }

    private async Task<SkillResult> GetControlsForPageAsync(string pageKey, CancellationToken cancellationToken)
    {
        var controls = await _uiControlRepository.GetByPageKeyAsync(pageKey, cancellationToken);

        if (controls.Count == 0)
        {
            return SkillResult.Error($"No controls found for page '{pageKey}'. Use page=\"list\" to see available pages.");
        }

        var controlDtos = controls.Select(c => new
        {
            c.ControlKey,
            c.Selector,
            c.SelectorType,
            c.ControlType,
            c.Label,
            c.Route,
            c.SortOrder,
            c.IsDynamic,
            c.SelectorPattern
        }).ToList();

        var resultData = new
        {
            PageKey = pageKey,
            Controls = controlDtos,
            TotalControls = controlDtos.Count
        };

        var message = $"Page '{pageKey}' has {controlDtos.Count} control(s).";
        return SkillResult.SuccessResult(resultData, message);
    }
}
