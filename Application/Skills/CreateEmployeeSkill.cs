// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class CreateEmployeeSkill : BaseSkill
{
    private readonly IClientRepository _clientRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "create_employee";

    public override string Description =>
        "Creates a new employee or customer in the system with all data (name, address, birthdate, contract, group). " +
        "For Swiss addresses, the canton is automatically detected from the postal code.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanCreateClients" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "firstName",
            "First name of the employee",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "lastName",
            "Last name of the employee",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "gender",
            "Gender of the employee",
            SkillParameterType.Enum,
            Required: true,
            EnumValues: new List<string> { "Male", "Female", "Intersexuality", "LegalEntity" }),
        new SkillParameter(
            "birthdate",
            "Birthdate in format YYYY-MM-DD",
            SkillParameterType.Date,
            Required: false),
        new SkillParameter(
            "street",
            "Street and house number",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "postalCode",
            "Postal code (e.g. 3097)",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "city",
            "City name (e.g. Liebefeld)",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "canton",
            "Swiss canton code (e.g. BE, ZH, SG). Auto-detected from postal code if not provided.",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "country",
            "Country name (e.g. Schweiz, Deutschland)",
            SkillParameterType.String,
            Required: false,
            DefaultValue: "Schweiz"),
        new SkillParameter(
            "email",
            "Email address",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "phone",
            "Phone number",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "company",
            "Company name (for business entities)",
            SkillParameterType.String,
            Required: false)
    };

    public CreateEmployeeSkill(
        IClientRepository clientRepository,
        IAddressRepository addressRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetRequiredString(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var genderStr = GetRequiredString(parameters, "gender");

        if (!Enum.TryParse<GenderEnum>(genderStr, true, out var gender))
        {
            return SkillResult.Error($"Invalid gender value: {genderStr}");
        }

        var birthdate = GetParameter<string>(parameters, "birthdate");
        var street = GetParameter<string>(parameters, "street");
        var postalCode = GetParameter<string>(parameters, "postalCode");
        var city = GetParameter<string>(parameters, "city");
        var canton = GetParameter<string>(parameters, "canton");
        var country = GetParameter<string>(parameters, "country", "Schweiz");
        var company = GetParameter<string>(parameters, "company");

        if (string.IsNullOrEmpty(canton) && !string.IsNullOrEmpty(postalCode))
        {
            canton = DetectCantonFromPostalCode(postalCode);
        }

        var client = new Client
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            Name = lastName,
            Gender = gender,
            Company = company,
            LegalEntity = gender == GenderEnum.LegalEntity,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        if (!string.IsNullOrEmpty(birthdate) && DateTime.TryParse(birthdate, out var birthdateValue))
        {
            client.Birthdate = birthdateValue;
        }

        await _clientRepository.Add(client);

        if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city))
        {
            var address = new Address
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Street = street ?? "",
                Zip = postalCode ?? "",
                City = city ?? "",
                State = canton ?? "",
                Country = country ?? "Schweiz",
                Type = AddressTypeEnum.Employee,
                ValidFrom = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = context.UserName
            };

            await _addressRepository.Add(address);
        }

        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            EmployeeId = client.Id,
            FirstName = firstName,
            LastName = lastName,
            Gender = gender.ToString(),
            Canton = canton,
            City = city,
            Country = country
        };

        var message = $"Employee {firstName} {lastName}" +
                      (!string.IsNullOrEmpty(canton) ? $" from {canton}" : "") +
                      " was successfully created.";

        return SkillResult.SuccessResult(resultData, message);
    }

    private static string? DetectCantonFromPostalCode(string postalCode)
    {
        if (string.IsNullOrEmpty(postalCode) || postalCode.Length < 1)
            return null;

        var firstDigit = postalCode[0];

        return firstDigit switch
        {
            '1' => postalCode.StartsWith("12") || postalCode.StartsWith("13") ? "VD" : "GE",
            '2' => postalCode.StartsWith("25") || postalCode.StartsWith("26") || postalCode.StartsWith("27") ? "NE" : "JU",
            '3' => "BE",
            '4' => postalCode.StartsWith("40") || postalCode.StartsWith("41") ? "BS" : "BL",
            '5' => "AG",
            '6' => postalCode.StartsWith("60") || postalCode.StartsWith("61") ? "LU" : "ZG",
            '7' => "GR",
            '8' => postalCode.StartsWith("85") || postalCode.StartsWith("86") ? "TG" : "ZH",
            '9' => postalCode.StartsWith("94") || postalCode.StartsWith("95") ? "SG" : "AR",
            _ => null
        };
    }
}
