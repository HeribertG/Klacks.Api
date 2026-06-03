// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a qualification master entry with multilingual name and description.
/// At least one of name_de / name_en / name_fr / name_it is required.
/// The legacy "name" parameter maps to name_de for backwards compatibility.
/// </summary>
/// <param name="name_de">German qualification name (e.g. "Führerschein")</param>
/// <param name="name_en">English qualification name (e.g. "Driving licence")</param>
/// <param name="name_fr">French qualification name (e.g. "Permis de conduire")</param>
/// <param name="name_it">Italian qualification name (e.g. "Patente di guida")</param>
/// <param name="description_de">Optional German description</param>
/// <param name="description_en">Optional English description</param>
/// <param name="description_fr">Optional French description</param>
/// <param name="description_it">Optional Italian description</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_qualification")]
public class CreateQualificationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateQualificationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var nameDe = GetParameter<string>(parameters, "name_de")
                     ?? GetParameter<string>(parameters, "name");
        var nameEn = GetParameter<string>(parameters, "name_en");
        var nameFr = GetParameter<string>(parameters, "name_fr");
        var nameIt = GetParameter<string>(parameters, "name_it");

        if (string.IsNullOrWhiteSpace(nameDe) && string.IsNullOrWhiteSpace(nameEn)
            && string.IsNullOrWhiteSpace(nameFr) && string.IsNullOrWhiteSpace(nameIt))
        {
            return SkillResult.Error("At least one language name (name_de, name_en, name_fr, or name_it) is required.");
        }

        var mlName = new MultiLanguage
        {
            De = nameDe?.Trim(),
            En = nameEn?.Trim(),
            Fr = nameFr?.Trim(),
            It = nameIt?.Trim(),
        };

        var descDe = GetParameter<string>(parameters, "description_de")
                     ?? GetParameter<string>(parameters, "description");
        var descEn = GetParameter<string>(parameters, "description_en");
        var descFr = GetParameter<string>(parameters, "description_fr");
        var descIt = GetParameter<string>(parameters, "description_it");

        MultiLanguage? mlDescription = null;
        if (!string.IsNullOrWhiteSpace(descDe) || !string.IsNullOrWhiteSpace(descEn)
            || !string.IsNullOrWhiteSpace(descFr) || !string.IsNullOrWhiteSpace(descIt))
        {
            mlDescription = new MultiLanguage
            {
                De = descDe?.Trim(),
                En = descEn?.Trim(),
                Fr = descFr?.Trim(),
                It = descIt?.Trim(),
            };
        }

        var id = await _mediator.Send(new CreateQualificationCommand(mlName, mlDescription), cancellationToken);
        var displayName = nameDe ?? nameEn ?? nameFr ?? nameIt ?? string.Empty;

        return SkillResult.SuccessResult(
            new { Id = id, Name = mlName },
            $"Qualification '{displayName.Trim()}' is available (id {id}).");
    }
}
