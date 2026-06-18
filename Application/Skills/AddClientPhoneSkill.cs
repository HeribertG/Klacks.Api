// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal, single-purpose skill: adds a phone number to an existing client identified by name.
/// Exists to keep the tool schema tiny so the LLM reliably emits the call (the large multi-field
/// update_client is not reliably invoked by the models for edit requests).
/// </summary>
/// <param name="firstName">First name of the client to update.</param>
/// <param name="lastName">Last name of the client to update.</param>
/// <param name="phone">Phone number to add.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_client_phone")]
public class AddClientPhoneSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddClientPhoneSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var phone = GetRequiredString(parameters, "phone");

        var term = $"{firstName} {lastName}".Trim();
        var search = await _searchRepository.SearchAsync(term, null, null, null, 5, cancellationToken);
        if (search.Items.Count == 0)
        {
            return SkillResult.Error($"No client found matching '{term}'.");
        }
        if (search.Items.Count > 1)
        {
            var names = string.Join(", ", search.Items.Select(i => $"{i.FirstName} {i.LastName} (#{i.IdNumber})"));
            return SkillResult.Error($"Multiple clients match '{term}': {names}. Please be more specific.");
        }

        var client = await _clientRepository.Get(search.Items[0].Id);
        if (client == null)
        {
            return SkillResult.Error($"Client '{term}' could not be loaded.");
        }

        var now = DateTime.UtcNow;
        client.Communications.Add(new Communication
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Type = CommunicationTypeEnum.PrivateCellPhone,
            Value = phone,
            CreateTime = now,
            CurrentUserCreated = context.UserName
        });
        client.UpdateTime = now;
        client.CurrentUserUpdated = context.UserName;

        await _clientRepository.Put(client);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name, Phone = phone },
            $"Phone {phone} added to {client.FirstName} {client.Name}.");
    }
}
