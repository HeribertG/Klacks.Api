// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that creates a new client (employee, external employee or customer) with the mandatory
/// onboarding data: client master record + membership, plus optional address and communication
/// (email/phone). The id_number is left at its CLR default so the database sequence assigns it.
/// </summary>
/// <param name="clientRepository">Persists the client and its cascaded child entities</param>
/// <param name="unitOfWork">Commits the unit of work</param>

using Klacks.Api.Application.Common;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

using Klacks.Api.Application.DTOs.Staffs;

using Klacks.Api.Application.Mappers;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_employee")]
public class CreateEmployeeSkill : BaseSkillImplementation
{
    private const string SkillName = "create_employee";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;
    private readonly ICountryResolver _countryResolver;
    private readonly IPendingConfirmationStore _confirmationStore;
    private readonly ICompanyClock _companyClock;

    public CreateEmployeeSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        ClientMapper clientMapper,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes,
        ICountryResolver countryResolver,
        IPendingConfirmationStore confirmationStore,
        ICompanyClock companyClock)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _clientMapper = clientMapper;
        _selfApi = selfApi;
        _routes = routes;
        _countryResolver = countryResolver;
        _confirmationStore = confirmationStore;
        _companyClock = companyClock;
    }

    private sealed record EmployeeInput(
        string FirstName,
        string LastName,
        GenderEnum Gender,
        EntityTypeEnum EntityType,
        string? Birthdate,
        string? Street,
        string? Zip,
        string? City,
        string? State,
        string? Country,
        string? Company,
        string? Email,
        string? Phone,
        string? MemberSince,
        bool ProceedWithoutContact);

    private sealed record ResolvedContact(string? State, string CountryCode, string? PhonePrefix, string? PhoneNumber);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (input, inputError) = ReadInput(parameters);
        if (input is null)
        {
            return SkillResult.Error(inputError!);
        }

        var (memberSinceDate, memberSinceError) = await ParseMemberSinceAsync(input.MemberSince, context, cancellationToken);
        if (memberSinceDate is null)
        {
            return SkillResult.Error(memberSinceError!);
        }

        var onboardingError = FindMissingOnboardingData(input);
        if (onboardingError != null)
        {
            return SkillResult.Error(onboardingError);
        }

        var contact = await ResolveContactAsync(input, cancellationToken);

        var (client, birthdateError) = BuildClient(input, contact, memberSinceDate.Value, context);
        if (client is null)
        {
            return SkillResult.Error(birthdateError!);
        }

        var reused = await TryReuseCustomerAsync(input, client, cancellationToken);
        if (reused != null)
        {
            return reused;
        }

        var confirmation = await RequireValidFromConfirmationAsync(context, parameters, client, cancellationToken);
        if (confirmation != null)
        {
            return confirmation;
        }

        return await PostClientAsync(input, contact, client, context, cancellationToken);
    }

    private static (EmployeeInput? Input, string? Error) ReadInput(Dictionary<string, object> parameters)
    {
        var firstName = GetRequiredString(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var genderStr = GetRequiredString(parameters, "gender");

        if (!Enum.TryParse<GenderEnum>(genderStr, true, out var gender))
        {
            return (null, $"Invalid gender value: {genderStr}");
        }

        var entityTypeStr = GetParameter<string>(parameters, "entityType");
        var entityType = EntityTypeEnum.Employee;
        if (!string.IsNullOrWhiteSpace(entityTypeStr)
            && !Enum.TryParse(entityTypeStr, true, out entityType))
        {
            return (null,
                $"Invalid entityType value: {entityTypeStr}. Valid values: Employee, ExternEmp, Customer.");
        }

        return (new EmployeeInput(
            firstName,
            lastName,
            gender,
            entityType,
            GetParameter<string>(parameters, "birthdate"),
            GetParameter<string>(parameters, "street"),
            GetParameter<string>(parameters, "zip"),
            GetParameter<string>(parameters, "city"),
            GetParameter<string>(parameters, "state"),
            GetParameter<string>(parameters, "country"),
            GetParameter<string>(parameters, "company"),
            GetParameter<string>(parameters, "email"),
            GetParameter<string>(parameters, "phone"),
            GetParameter<string>(parameters, "memberSince"),
            GetParameter<bool>(parameters, "proceedWithoutContact", false)), null);
    }

    private async Task<(DateTime? MemberSince, string? Error)> ParseMemberSinceAsync(
        string? memberSince, SkillExecutionContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(memberSince))
        {
            return (null,
                "Cannot create the client yet. Please ask the user when the employee starts working " +
                "(memberSince / Eintrittsdatum / start date), then call create_employee again with that value in YYYY-MM-DD format. " +
                "When you ask for this date, append a date-picker block so the user can pick it instead of typing: [REPLIES:date \"Ab wann?\"]. " +
                "Resolve casual phrases the user already gave you: \"heute\"/\"sofort\"/\"jetzt\"/\"direkt\"/\"today\"/\"now\" → today's date; " +
                "\"morgen\"/\"tomorrow\" → tomorrow's date; \"1. Juli\"/\"July 1st\" → the matching YYYY-MM-DD.");
        }

        var (parsedMemberSince, invalidMemberSince) = SkillDateParser.ParseOptionalUtcDate(
            memberSince, await _companyClock.GetTodayAsync(cancellationToken), context.UserLanguage);
        if (invalidMemberSince || parsedMemberSince is null)
        {
            return (null, SkillDateParser.InvalidDateMessageFor("memberSince", memberSince));
        }

        return (parsedMemberSince.Value, null);
    }

    private static string? FindMissingOnboardingData(EmployeeInput input)
    {
        var (street, zip, city, email, phone) = (input.Street, input.Zip, input.City, input.Email, input.Phone);

        // An address must ALWAYS be complete — a partial address (e.g. a missing zip / postal code) is
        // invalid even when contact data is skipped. proceedWithoutContact only waives email/phone.
        var hasAnyAddress = !string.IsNullOrWhiteSpace(street)
                            || !string.IsNullOrWhiteSpace(zip)
                            || !string.IsNullOrWhiteSpace(city);
        if (hasAnyAddress
            && (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(zip) || string.IsNullOrWhiteSpace(city)))
        {
            return
                "Cannot create the client yet. The address is incomplete: street, zip (postal code) and city are all " +
                "required when an address is given. The zip/postal code is missing — ask the user for it (e.g. 2500 for Biel) " +
                "or supply the one you already know, then call create_employee again with the complete address.";
        }

        if (input.ProceedWithoutContact)
        {
            return null;
        }

        var missing = new List<string>();
        if (!hasAnyAddress)
        {
            missing.Add("address (street, zip, city)");
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            missing.Add("email");
        }
        if (string.IsNullOrWhiteSpace(phone))
        {
            missing.Add("phone");
        }

        return missing.Count > 0
            ? "Cannot create the client yet. The following onboarding data is still missing: " +
              string.Join(", ", missing) +
              ". Ask the user for each missing item, then call create_employee again with all collected fields. " +
              "Only if the user explicitly declines to provide them, call create_employee again with proceedWithoutContact set to true."
            : null;
    }

    private async Task<ResolvedContact> ResolveContactAsync(EmployeeInput input, CancellationToken cancellationToken)
    {
        var (state, zip) = (input.State, input.Zip);

        var resolvedCountry = await _countryResolver.ResolveAsync(input.Country, cancellationToken)
            ?? await _countryResolver.GetDefaultAsync(cancellationToken);

        var countryCode = resolvedCountry?.Abbreviation ?? string.Empty;

        if (string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(zip))
        {
            state = await _searchRepository.FindStatePostCode(zip!);
        }

        var (phonePrefix, phoneNumber) = PhoneNumberSplitter.Split(input.Phone, resolvedCountry?.Prefix);

        return new ResolvedContact(state, countryCode, phonePrefix, phoneNumber);
    }

    private static (Client? Client, string? Error) BuildClient(
        EmployeeInput input, ResolvedContact contact, DateTime memberSinceDate, SkillExecutionContext context)
    {
        var (gender, birthdate) = (input.Gender, input.Birthdate);

        var now = DateTime.UtcNow;

        var client = new Client
        {
            Id = Guid.NewGuid(),
            FirstName = input.FirstName,
            Name = input.LastName,
            Gender = gender,
            Company = input.Company,
            Type = input.EntityType,
            LegalEntity = gender == GenderEnum.LegalEntity,
            CreateTime = now,
            CurrentUserCreated = context.UserName
        };

        if (!string.IsNullOrEmpty(birthdate))
        {
            if (!SkillUtcDateTimeParser.TryParse(birthdate, context.UserLanguage, out var birthdateValue))
            {
                return (null, SkillDateParser.InvalidBirthdateMessage(birthdate));
            }

            client.Birthdate = birthdateValue;
        }

        client.Membership = new Membership
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            ValidFrom = DateTime.SpecifyKind(memberSinceDate.Date, DateTimeKind.Utc),
            CreateTime = now,
            CurrentUserCreated = context.UserName
        };

        AddAddress(client, input, contact, now, context.UserName);
        AddCommunications(client, input.Email, contact, now, context.UserName);

        return (client, null);
    }

    private static void AddAddress(Client client, EmployeeInput input, ResolvedContact contact, DateTime now, string userName)
    {
        var (street, zip, city) = (input.Street, input.Zip, input.City);

        if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(zip))
        {
            client.Addresses.Add(new Address
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Street = street ?? string.Empty,
                Zip = zip ?? string.Empty,
                City = city ?? string.Empty,
                State = contact.State ?? string.Empty,
                Country = contact.CountryCode,
                Type = AddressTypeEnum.Employee,
                ValidFrom = now,
                CreateTime = now,
                CurrentUserCreated = userName
            });
        }
    }

    private static void AddCommunications(Client client, string? email, ResolvedContact contact, DateTime now, string userName)
    {
        var (phonePrefix, phoneNumber) = (contact.PhonePrefix, contact.PhoneNumber);

        if (!string.IsNullOrEmpty(email))
        {
            client.Communications.Add(new Communication
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = CommunicationTypeEnum.PrivateMail,
                Value = email,
                CreateTime = now,
                CurrentUserCreated = userName
            });
        }

        if (!string.IsNullOrEmpty(phoneNumber))
        {
            client.Communications.Add(new Communication
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = CommunicationTypeEnum.PrivateCellPhone,
                Prefix = phonePrefix,
                Value = phoneNumber,
                CreateTime = now,
                CurrentUserCreated = userName
            });
        }
    }

    private async Task<SkillResult?> TryReuseCustomerAsync(
        EmployeeInput input, Client client, CancellationToken cancellationToken)
    {
        var (entityType, street, zip) = (input.EntityType, input.Street, input.Zip);

        // CUS-6 / HIGH-1 customer idempotency: when the model re-plans on "weiter" it may re-create a customer
        // it already created (function results do not persist across turns); without a deterministic guard the
        // order's ClientId guard would then miss and a duplicate customer (and order) would result. Reuse an
        // existing customer with the same business key (company + zip + street) instead of creating a duplicate.
        // The guard lives in the repository (data access); only customers are deduped — employees are real
        // people and two with the same name/address are legitimately distinct.
        if (entityType != EntityTypeEnum.Customer)
        {
            return null;
        }

        var existingCustomer = await _clientRepository.FindReusableCustomerAsync(client, cancellationToken);
        if (existingCustomer == null)
        {
            return null;
        }

        var existingLabel = !string.IsNullOrWhiteSpace(existingCustomer.Company)
            ? existingCustomer.Company
            : $"{existingCustomer.FirstName} {existingCustomer.Name}".Trim();

        return SkillResult.SuccessResult(
            new
            {
                EmployeeId = existingCustomer.Id,
                ClientId = existingCustomer.Id,
                FirstName = existingCustomer.FirstName,
                LastName = existingCustomer.Name,
                Company = existingCustomer.Company,
                EntityType = entityType.ToString(),
                Reused = true
            },
            $"Customer '{existingLabel}' already exists at {street}, {zip} — reusing it, no duplicate created. " +
            "Use this customer id (clientId) for the order with create_shift; do NOT create another customer.");
    }

    private async Task<SkillResult?> RequireValidFromConfirmationAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        Client client,
        CancellationToken cancellationToken)
    {
        var validFromConfirmed = GetParameter<bool>(
            parameters, MembershipPlausibilityDefaults.ValidFromConfirmedParameter, false);
        if (validFromConfirmed)
        {
            return null;
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var reason = MembershipValidFromPlausibility.Evaluate(
            client.Membership!.ValidFrom, client.Birthdate, today);
        return reason != null
            ? ValidFromConfirmationFactory.RequireConfirmation(
                _confirmationStore, context.UserId, SkillName, parameters, reason, context.ConfirmationChannel)
            : null;
    }

    private async Task<SkillResult> PostClientAsync(
        EmployeeInput input,
        ResolvedContact contact,
        Client client,
        SkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        var (firstName, lastName, entityType) = (input.FirstName, input.LastName, input.EntityType);

        var result = await _selfApi.PostAsync<ClientResource>(
            _routes.Resolve(typeof(ClientResource)), _clientMapper.ToResource(client), context,
            SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        if (result.Value == null)
        {
            return SkillResult.Error(
                $"Creating the {entityType} '{firstName} {lastName}' returned no result — " +
                "the operation may have failed.");
        }

        client.Id = result.Value.Id;

        return CreatedClientResult(input, contact, client.Id);
    }

    private static SkillResult CreatedClientResult(EmployeeInput input, ResolvedContact contact, Guid persistedId)
    {
        var (firstName, lastName, entityType) = (input.FirstName, input.LastName, input.EntityType);
        var (email, phone, state) = (input.Email, input.Phone, contact.State);

        var resultData = new
        {
            EmployeeId = persistedId,
            FirstName = firstName,
            LastName = lastName,
            Gender = input.Gender.ToString(),
            EntityType = entityType.ToString(),
            HasMembership = true,
            HasEmail = !string.IsNullOrEmpty(email),
            HasPhone = !string.IsNullOrEmpty(phone),
            State = state,
            City = input.City,
            Country = contact.CountryCode
        };

        var message = $"{entityType} {firstName} {lastName}" +
                      (!string.IsNullOrEmpty(state) ? $" from {state}" : string.Empty) +
                      " was successfully created (with membership" +
                      (!string.IsNullOrEmpty(email) ? ", email" : string.Empty) +
                      (!string.IsNullOrEmpty(phone) ? ", phone" : string.Empty) +
                      ") and confirmed in the database (verified).";

        return SkillResult.SuccessResult(resultData, message);
    }
}
