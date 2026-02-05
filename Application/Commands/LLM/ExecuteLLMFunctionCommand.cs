using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Application.Commands.LLM;

public class ExecuteLLMFunctionCommand : IRequest<LLMFunctionResult>
{
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class ExecuteLLMFunctionCommandHandler : IRequestHandler<ExecuteLLMFunctionCommand, LLMFunctionResult>
{
    private readonly ILogger<ExecuteLLMFunctionCommandHandler> _logger;
    private readonly IClientRepository _clientRepository;

    public ExecuteLLMFunctionCommandHandler(
        ILogger<ExecuteLLMFunctionCommandHandler> logger,
        IClientRepository clientRepository)
    {
        _logger = logger;
        _clientRepository = clientRepository;
    }

    public async Task<LLMFunctionResult> Handle(ExecuteLLMFunctionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing function {FunctionName}", request.FunctionName);

        try
        {
            return request.FunctionName switch
            {
                "create_client" => await ExecuteCreateClientAsync(request.Parameters ?? new()),
                "search_clients" => await ExecuteSearchClientsAsync(request.Parameters ?? new()),
                "get_system_info" => ExecuteGetSystemInfo(),
                _ => new LLMFunctionResult
                {
                    Success = false,
                    Error = $"Unknown function: {request.FunctionName}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", request.FunctionName);
            return new LLMFunctionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private async Task<LLMFunctionResult> ExecuteCreateClientAsync(Dictionary<string, object> parameters)
    {
        var firstName = parameters.GetValueOrDefault("firstName")?.ToString() ?? "";
        var lastName = parameters.GetValueOrDefault("lastName")?.ToString() ?? "";
        var gender = parameters.GetValueOrDefault("gender")?.ToString() ?? "Male";
        var birthdateStr = parameters.GetValueOrDefault("birthdate")?.ToString();
        var street = parameters.GetValueOrDefault("street")?.ToString();
        var postalCode = parameters.GetValueOrDefault("postalCode")?.ToString();
        var city = parameters.GetValueOrDefault("city")?.ToString();
        var canton = parameters.GetValueOrDefault("canton")?.ToString();
        var country = parameters.GetValueOrDefault("country")?.ToString() ?? "Schweiz";

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return new LLMFunctionResult
            {
                Success = false,
                Error = "First name and last name are required"
            };
        }

        var detectedCanton = canton ?? GetCantonFromPostalCode(postalCode);

        var client = new Client
        {
            FirstName = firstName,
            Name = lastName,
            Gender = ParseGender(gender),
            Type = EntityTypeEnum.Employee
        };

        if (!string.IsNullOrWhiteSpace(birthdateStr) && DateTime.TryParse(birthdateStr, out var birthdate))
        {
            client.Birthdate = birthdate;
        }

        if (!string.IsNullOrWhiteSpace(street) || !string.IsNullOrWhiteSpace(city))
        {
            var address = new Address
            {
                ClientId = client.Id,
                Street = street ?? "",
                Zip = postalCode ?? "",
                City = city ?? "",
                State = detectedCanton ?? "",
                Country = country ?? "Schweiz",
                Type = AddressTypeEnum.Employee,
                ValidFrom = DateTime.UtcNow
            };
            client.Addresses.Add(address);
        }

        await _clientRepository.Add(client);

        var message = $"Mitarbeiter {firstName} {lastName} wurde erfolgreich erstellt.";
        if (client.Birthdate.HasValue)
        {
            message += $" Geburtsdatum: {client.Birthdate.Value:dd.MM.yyyy}.";
        }
        if (client.Addresses.Any())
        {
            var addr = client.Addresses.First();
            message += $" Adresse: {addr.Street}, {addr.Zip} {addr.City}";
            if (!string.IsNullOrWhiteSpace(addr.State))
            {
                message += $", Kanton {addr.State}";
            }
            message += $", {addr.Country}.";
        }

        return new LLMFunctionResult
        {
            Success = true,
            Message = message,
            Result = new
            {
                Id = client.Id,
                FirstName = client.FirstName,
                LastName = client.Name,
                Birthdate = client.Birthdate?.ToString("yyyy-MM-dd"),
                Address = client.Addresses.FirstOrDefault() != null ? new
                {
                    client.Addresses.First().Street,
                    PostalCode = client.Addresses.First().Zip,
                    client.Addresses.First().City,
                    Canton = client.Addresses.First().State,
                    client.Addresses.First().Country
                } : null
            }
        };
    }

    private static string? GetCantonFromPostalCode(string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode) || !int.TryParse(postalCode, out var plz))
            return null;

        return plz switch
        {
            >= 1000 and < 2000 => "VD",
            >= 2000 and < 3000 => "NE",
            >= 3000 and < 4000 => "BE",
            >= 4000 and < 5000 => "BS",
            >= 5000 and < 6000 => "AG",
            >= 6000 and < 7000 => "LU",
            >= 7000 and < 8000 => "GR",
            >= 8000 and < 9000 => "ZH",
            >= 9000 and < 10000 => "SG",
            _ => null
        };
    }

    private async Task<LLMFunctionResult> ExecuteSearchClientsAsync(Dictionary<string, object> parameters)
    {
        var searchTerm = parameters.GetValueOrDefault("searchTerm")?.ToString() ?? "";

        var clients = await _clientRepository.List();
        var filtered = clients
            .Where(c => string.IsNullOrEmpty(searchTerm) ||
                        (c.FirstName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                        (c.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                        (c.Company?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true))
            .Take(10)
            .ToList();

        return new LLMFunctionResult
        {
            Success = true,
            Result = filtered.Select(c => new
            {
                c.Id,
                c.FirstName,
                LastName = c.Name,
                c.Company
            }).ToList()
        };
    }

    private static LLMFunctionResult ExecuteGetSystemInfo()
    {
        return new LLMFunctionResult
        {
            Success = true,
            Result = new
            {
                SystemName = "Klacks Planning System",
                Version = "2.5.0",
                Status = "Active"
            }
        };
    }

    private static GenderEnum ParseGender(string gender)
    {
        return gender.ToLower() switch
        {
            "male" => GenderEnum.Male,
            "female" => GenderEnum.Female,
            "intersexuality" => GenderEnum.Intersexuality,
            "legalentity" => GenderEnum.LegalEntity,
            _ => GenderEnum.Male
        };
    }
}
