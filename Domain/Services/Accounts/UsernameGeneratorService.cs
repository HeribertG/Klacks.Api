// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Domain.Services.Accounts;

public interface IUsernameGeneratorService
{
    Task<string> GenerateUniqueUsernameAsync(string firstName, string lastName);
}

public class UsernameGeneratorService : IUsernameGeneratorService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UsernameGeneratorService> _logger;

    public UsernameGeneratorService(
        UserManager<AppUser> userManager,
        ILogger<UsernameGeneratorService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<string> GenerateUniqueUsernameAsync(string firstName, string lastName)
    {
        var baseUsername = CreateBaseUsername(firstName, lastName);
        var username = baseUsername;
        var counter = 1;

        while (await UsernameExistsAsync(username))
        {
            username = $"{baseUsername}{counter}";
            counter++;

            if (counter > 999)
            {
                username = $"{baseUsername}{Guid.NewGuid().ToString()[..4]}";
                break;
            }
        }

        _logger.LogInformation("Generated unique username: {Username} from {FirstName} {LastName}",
            username, firstName, lastName);

        return username;
    }

    private static string CreateBaseUsername(string firstName, string lastName)
    {
        var first = NormalizeString(firstName);
        var last = NormalizeString(lastName);

        if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
        {
            return "usr";
        }

        if (string.IsNullOrEmpty(last))
        {
            return first.Length >= 3 ? first[..3].ToLowerInvariant() : PadToThree(first);
        }

        if (string.IsNullOrEmpty(first))
        {
            return last.Length >= 3 ? last[..3].ToLowerInvariant() : PadToThree(last);
        }

        // First letter of firstName + first 2 letters of lastName (e.g., "Max Mustermann" -> "mmu")
        var result = first[0].ToString();
        result += last.Length >= 2 ? last[..2] : last + "x";

        return result.ToLowerInvariant();
    }

    private static string PadToThree(string input)
    {
        var result = input.ToLowerInvariant();
        while (result.Length < 3)
        {
            result += "x";
        }
        return result;
    }

    private static string NormalizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.Trim()
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("ü", "ue")
            .Replace("ß", "ss")
            .Replace("Ä", "Ae")
            .Replace("Ö", "Oe")
            .Replace("Ü", "Ue");

        return new string(normalized.Where(c => char.IsLetterOrDigit(c)).ToArray());
    }

    private async Task<bool> UsernameExistsAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        return user != null;
    }
}
