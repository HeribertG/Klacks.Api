// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates an individual period scheme via PostCommand. The stretches arrive as a JSON array, the
/// same shape import_calendar_rules uses, because the flat parameter list of the skill layer cannot
/// express a nested collection.
/// </summary>
/// <param name="name">Name of the scheme (required).</param>
/// <param name="periodsJson">JSON array of { fromDate, untilDate?, fullHours } objects.</param>

using System.Text.Json;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_individual_period")]
public class CreateIndividualPeriodSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateIndividualPeriodSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    private sealed record StretchInput(string FromDate, string? UntilDate, decimal FullHours);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, "name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("Parameter 'name' is required.");
        }

        var periods = new List<PeriodResource>();
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

                periods.Add(new PeriodResource
                {
                    FromDate = fromDate,
                    UntilDate = untilDate,
                    FullHours = input.FullHours
                });
            }
        }

        var resource = new IndividualPeriodResource { Name = name, Periods = periods };

        IndividualPeriodResource? created;
        try
        {
            created = await _mediator.Send(new PostCommand<IndividualPeriodResource>(resource), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (created == null)
        {
            return SkillResult.Error($"Creating individual period scheme '{name}' returned no result.");
        }

        return SkillResult.SuccessResult(
            new { created.Id, created.Name, StretchCount = created.Periods.Count },
            $"Individual period scheme '{created.Name}' created with {created.Periods.Count} stretch(es) (id {created.Id}).");
    }
}
