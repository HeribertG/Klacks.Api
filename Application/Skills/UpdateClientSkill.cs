// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing client (employee, customer or extern employee) by id. Only the
/// fields supplied as parameters are changed; everything else stays untouched. Master fields
/// are overwritten in place; a supplied address or email/phone is ADDED as a new (versioned)
/// entry so the client's history is preserved.
/// </summary>
/// <param name="clientId">Required. UUID of the client to update.</param>
/// <param name="firstName">Optional. New first name.</param>
/// <param name="lastName">Optional. New last name (Client.Name).</param>
/// <param name="title">Optional. Title (e.g. Dr.).</param>
/// <param name="company">Optional. Company name (for legal entities).</param>
/// <param name="birthdate">Optional. Birthdate in YYYY-MM-DD.</param>
/// <param name="gender">Optional. Male / Female / Intersexuality / LegalEntity.</param>
/// <param name="street">Optional. Adds a new address (street).</param>
/// <param name="email">Optional. Adds a new email communication.</param>
/// <param name="phone">Optional. Adds a new phone communication.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_client")]
public class UpdateClientSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateClientSkill> _logger;
    private readonly ICountryResolver _countryResolver;

    public UpdateClientSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateClientSkill> logger,
        ICountryResolver countryResolver)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _countryResolver = countryResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (client, resolveError) = await ResolveClientAsync(parameters, cancellationToken);
        if (resolveError != null)
        {
            return SkillResult.Error(resolveError);
        }

        var clientId = client!.Id;

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

        var now = DateTime.UtcNow;

        var street = GetParameter<string>(parameters, "street");
        var zip = GetParameter<string>(parameters, "zip");
        var city = GetParameter<string>(parameters, "city");
        var stateParam = GetParameter<string>(parameters, "state");
        var countryInput = GetParameter<string>(parameters, "country");
        if (!string.IsNullOrWhiteSpace(street) || !string.IsNullOrWhiteSpace(zip) || !string.IsNullOrWhiteSpace(city))
        {
            var resolvedCountry = await _countryResolver.ResolveAsync(countryInput, cancellationToken)
                ?? await _countryResolver.GetDefaultAsync(cancellationToken);
            var countryCode = resolvedCountry?.Abbreviation ?? string.Empty;

            client.Addresses.Add(new Address
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Street = street ?? string.Empty,
                Zip = zip ?? string.Empty,
                City = city ?? string.Empty,
                State = stateParam ?? string.Empty,
                Country = countryCode,
                Type = client.Type == EntityTypeEnum.Customer ? AddressTypeEnum.Workplace : AddressTypeEnum.Employee,
                ValidFrom = now,
                CreateTime = now,
                CurrentUserCreated = context.UserName
            });
            changed.Add("address");
        }

        var email = GetParameter<string>(parameters, "email");
        if (!string.IsNullOrWhiteSpace(email))
        {
            client.Communications.Add(new Communication
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = CommunicationTypeEnum.PrivateMail,
                Value = email,
                CreateTime = now,
                CurrentUserCreated = context.UserName
            });
            changed.Add("email");
        }

        var phone = GetParameter<string>(parameters, "phone");
        if (!string.IsNullOrWhiteSpace(phone))
        {
            client.Communications.Add(new Communication
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = CommunicationTypeEnum.PrivateCellPhone,
                Value = phone,
                CreateTime = now,
                CurrentUserCreated = context.UserName
            });
            changed.Add("phone");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ClientId = clientId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — client left unchanged.");
        }

        client.UpdateTime = DateTime.UtcNow;
        client.CurrentUserUpdated = context.UserName;

        try
        {
            await _clientRepository.Put(client);
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "update_client save failed for client {ClientId}", clientId);
            var detail = ex.InnerException?.Message ?? ex.Message;
            return SkillResult.Error($"Failed to save client update: {detail}");
        }

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

    private async Task<(Client? Client, string? Error)> ResolveClientAsync(
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var clientIdValue = GetParameter<string>(parameters, "clientId");
        if (!string.IsNullOrWhiteSpace(clientIdValue) && Guid.TryParse(clientIdValue, out var clientId))
        {
            var byId = await _clientRepository.Get(clientId);
            return byId == null
                ? (null, $"Client with ID '{clientId}' not found.")
                : (byId, null);
        }

        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetParameter<string>(parameters, "lastName");
        var term = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            return (null, "Provide either clientId or the client's first and last name to identify the client.");
        }

        var search = await _searchRepository.SearchAsync(term, null, null, null, 5, cancellationToken);
        if (search.Items.Count == 0)
        {
            return (null, $"No client found matching '{term}'.");
        }

        if (search.Items.Count > 1)
        {
            var names = string.Join(", ", search.Items.Select(i => $"{i.FirstName} {i.LastName} (#{i.IdNumber})"));
            return (null, $"Multiple clients match '{term}': {names}. Please specify which one.");
        }

        var full = await _clientRepository.Get(search.Items[0].Id);
        return full == null
            ? (null, $"Client '{term}' could not be loaded.")
            : (full, null);
    }
}
