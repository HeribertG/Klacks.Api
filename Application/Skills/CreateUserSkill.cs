// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Cryptography;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_user")]
public class CreateUserSkill : BaseSkillImplementation
{
    private readonly IUserManagementService _userManagementService;
    private readonly IUsernameGeneratorService _usernameGeneratorService;

    public CreateUserSkill(
        IUserManagementService userManagementService,
        IUsernameGeneratorService usernameGeneratorService)
    {
        _userManagementService = userManagementService;
        _usernameGeneratorService = usernameGeneratorService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetRequiredString(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var email = GetRequiredString(parameters, "email");

        var existingUser = await _userManagementService.FindUserByEmailAsync(email);
        if (existingUser != null)
        {
            return SkillResult.Error($"A user with email '{email}' already exists.");
        }

        var username = await _usernameGeneratorService.GenerateUniqueUsernameAsync(firstName, lastName);
        var password = GeneratePassword();

        var user = new AppUser
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            UserName = username
        };

        var (success, result) = await _userManagementService.RegisterUserAsync(user, password);

        if (!success)
        {
            var errors = result?.Errors != null
                ? string.Join(", ", result.Errors.Select(e => e.Description))
                : "Registration failed";
            return SkillResult.Error($"Failed to create user: {errors}");
        }

        var resultData = new
        {
            UserId = user.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            UserName = username
        };

        return SkillResult.SuccessResult(resultData,
            $"User '{firstName} {lastName}' ({email}) was successfully created with username '{username}'. " +
            "A strong password was generated automatically and is intentionally not shown for security reasons. " +
            "The new user must set their own password via the password-reset link on the login page.");
    }

    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "@$!%*?&";

        var password = new char[12];

        password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        var allChars = upper + lower + digits + special;
        for (var i = 4; i < password.Length; i++)
        {
            password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        for (var i = password.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
