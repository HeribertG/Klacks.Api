// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an individual period scheme: loads it via GetQuery, patches the name and, when
/// periodsJson is supplied, replaces the whole set of stretches. Replacing rather than merging keeps
/// the operation predictable — a scheme is only meaningful as a complete, gap-free sequence.
/// </summary>
/// <param name="individualPeriodId">UUID of the scheme to update (required).</param>
/// <param name="name">Optional new name.</param>
/// <param name="periodsJson">Optional JSON array of { fromDate, untilDate?, fullHours }; replaces all stretches.</param>

using System.Text.Json;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_individual_period")]
public class UpdateIndividualPeriodSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateIndividualPeriodSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    private sealed record StretchInput(string FromDate, string? UntilDate, decimal FullHours);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var individualPeriodId = GetRequiredGuid(parameters, "individualPeriodId");

        IndividualPeriodResource existing;
        try
        {
            existing = await _mediator.Send(
                new GetQuery<IndividualPeriodResource>(individualPeriodId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Individual period scheme {individualPeriodId} not found.");
        }

        var changed = new List<string>();

        var name = GetParameter<string>(parameters, "name");
        if (name != null && name.Trim() != existing.Name)
        {
            existing.Name = name.Trim();
            changed.Add("name");
        }

        var periodsJson = GetParameter<string>(parameters, "periodsJson");
        if (!string.IsNullOrWhiteSpace(periodsJson))
        {
            List<StretchInput> inputs;
            try
            {
                inputs = JsonSerializer.Deserialize<List<StretchInput>>(
                    periodsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
            catch (JsonException exception)
            {
                return SkillResult.Error($"periodsJson is not a valid JSON array: {exception.Message}");
            }

            var replacement = new List<PeriodResource>();
            foreach (var input in inputs)
            {
                if (!DateOnly.TryParse(input.FromDate, out var fromDate))
                {
                    return SkillResult.Error($"fromDate '{input.FromDate}' is not a valid date (expected yyyy-MM-dd).");
                }

                DateOnly? untilDate = null;
                if (!string.IsNullOrWhiteSpace(input.UntilDate))
                {
                    if (!DateOnly.TryParse(input.UntilDate, out var parsedUntil))
                    {
                        return SkillResult.Error($"untilDate '{input.UntilDate}' is not a valid date (expected yyyy-MM-dd).");
                    }

                    untilDate = parsedUntil;
                }

                replacement.Add(new PeriodResource
                {
                    IndividualPeriodId = individualPeriodId,
                    FromDate = fromDate,
                    UntilDate = untilDate,
                    FullHours = input.FullHours
                });
            }

            existing.Periods = replacement;
            changed.Add("periods");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { IndividualPeriodId = individualPeriodId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — individual period scheme left unchanged.");
        }

        IndividualPeriodResource? updated;
        try
        {
            updated = await _mediator.Send(new PutCommand<IndividualPeriodResource>(existing), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (updated == null)
        {
            return SkillResult.Error(
                $"Update of individual period scheme {individualPeriodId} returned no result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                IndividualPeriodId = individualPeriodId,
                ChangedFields = changed,
                updated.Name,
                StretchCount = updated.Periods.Count
            },
            $"Individual period scheme '{updated.Name}' updated ({string.Join(", ", changed)}).");
    }
}
