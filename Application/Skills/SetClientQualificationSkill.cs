// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Sets (upserts) a qualification an employee holds, with a level and optional validity window.
/// Thin wrapper around <see cref="Klacks.Api.Application.Commands.Qualifications.SetClientQualificationCommand"/>.
/// Used so eligibility (find_replacement / the pre-commit guardrail) can know who is qualified for a
/// shift. Calling it again for the same employee + qualification updates the existing entry.
/// An employee hidden from the caller by group visibility gets the same answer as an unknown id.
/// </summary>
/// <param name="clientId">Required. UUID of the employee.</param>
/// <param name="qualificationId">Required. UUID of an existing qualification.</param>
/// <param name="level">Required. Proficiency 1=Low, 2=Basic, 3=Proficient, 4=Advanced, 5=Expert.</param>
/// <param name="validFrom">Optional. ISO date yyyy-MM-dd the qualification becomes valid.</param>
/// <param name="validUntil">Optional. ISO date yyyy-MM-dd the qualification expires.</param>
/// <param name="note">Optional. Free-text note.</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_client_qualification")]
public class SetClientQualificationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public SetClientQualificationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var qualificationId = GetRequiredGuid(parameters, "qualificationId");
        var level = GetRequiredInt(parameters, "level");
        if (!Enum.IsDefined(typeof(QualificationLevel), level))
        {
            return SkillResult.Error("level must be between 1 (Low) and 5 (Expert).");
        }

        var validFrom = GetParameter<DateOnly?>(parameters, "validFrom");
        var validUntil = GetParameter<DateOnly?>(parameters, "validUntil");
        if (validFrom.HasValue && validUntil.HasValue && validUntil < validFrom)
        {
            return SkillResult.Error("validUntil must be on or after validFrom.");
        }
        var note = GetParameter<string>(parameters, "note");

        try
        {
            var id = await _mediator.Send(
                new SetClientQualificationCommand(
                    clientId, qualificationId, (QualificationLevel)level, validFrom, validUntil, note),
                cancellationToken);

            return SkillResult.SuccessResult(
                new { Id = id, ClientId = clientId, QualificationId = qualificationId, Level = ((QualificationLevel)level).ToString() },
                $"Qualification set for employee {clientId} at level {(QualificationLevel)level}.");
        }
        catch (Exception ex) when (ex is DbUpdateException or KeyNotFoundException)
        {
            return SkillResult.Error("Could not set the qualification — the employee or qualification id may not exist.");
        }
    }
}
