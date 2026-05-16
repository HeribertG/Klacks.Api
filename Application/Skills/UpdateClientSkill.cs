// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing client (employee, customer or extern employee) by id. Only the
/// fields supplied as parameters are changed; everything else stays untouched. Contact
/// data and address are managed via separate skills/UI.
/// </summary>
/// <param name="clientId">Required. UUID of the client to update.</param>
/// <param name="firstName">Optional. New first name.</param>
/// <param name="lastName">Optional. New last name (Client.Name).</param>
/// <param name="title">Optional. Title (e.g. Dr.).</param>
/// <param name="company">Optional. Company name (for legal entities).</param>
/// <param name="birthdate">Optional. Birthdate in YYYY-MM-DD.</param>
/// <param name="gender">Optional. Male / Female / Intersexuality / LegalEntity.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_client")]
public class UpdateClientSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClientSkill(IClientRepository clientRepository, IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");

        var client = await _clientRepository.Get(clientId);
        if (client == null)
        {
            return SkillResult.Error($"Client with ID '{clientId}' not found.");
        }

        var changed = new List<string>();

        var firstName = GetParameter<string>(parameters, "firstName");
        if (!string.IsNullOrWhiteSpace(firstName) && firstName != client.FirstName)
        {
            client.FirstName = firstName;
            changed.Add("firstName");
        }

        var lastName = GetParameter<string>(parameters, "lastName");
        if (!string.IsNullOrWhiteSpace(lastName) && lastName != client.Name)
        {
            client.Name = lastName;
            changed.Add("lastName");
        }

        var title = GetParameter<string>(parameters, "title");
        if (title != null && title != client.Title)
        {
            client.Title = title;
            changed.Add("title");
        }

        var company = GetParameter<string>(parameters, "company");
        if (company != null && company != client.Company)
        {
            client.Company = company;
            changed.Add("company");
        }

        var birthdate = GetParameter<string>(parameters, "birthdate");
        if (!string.IsNullOrEmpty(birthdate) && DateTime.TryParse(birthdate, out var parsedBirthdate))
        {
            if (client.Birthdate != parsedBirthdate)
            {
                client.Birthdate = parsedBirthdate;
                changed.Add("birthdate");
            }
        }

        var genderStr = GetParameter<string>(parameters, "gender");
        if (!string.IsNullOrWhiteSpace(genderStr))
        {
            if (!Enum.TryParse<GenderEnum>(genderStr, true, out var gender))
            {
                return SkillResult.Error($"Invalid gender value: {genderStr}");
            }
            if (client.Gender != gender)
            {
                client.Gender = gender;
                client.LegalEntity = gender == GenderEnum.LegalEntity;
                changed.Add("gender");
            }
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ClientId = clientId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — client left unchanged.");
        }

        client.UpdateTime = DateTime.UtcNow;
        client.CurrentUserUpdated = context.UserName;

        await _clientRepository.Put(client);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                ChangedFields = changed,
                client.FirstName,
                LastName = client.Name,
                EntityType = client.Type.ToString()
            },
            $"Client '{client.FirstName} {client.Name}' updated ({string.Join(", ", changed)}).");
    }
}
