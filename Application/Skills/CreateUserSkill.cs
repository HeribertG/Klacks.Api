using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class CreateUserSkill : BaseSkill
{
    private readonly IUserManagementService _userManagementService;
    private readonly IUsernameGeneratorService _usernameGeneratorService;

    public override string Name => "create_user";

    public override string Description =>
        "Creates a new system user account (login) in the user administration. " +
        "This is for authentication accounts, not HR employees. " +
        "Requires admin permissions. A username is auto-generated from first and last name. " +
        "A random password is generated and returned.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { Permissions.CanEditSettings };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "firstName",
            "First name of the user",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "lastName",
            "Last name of the user",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "email",
            "Email address of the user",
            SkillParameterType.String,
            Required: true)
    };

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
            UserName = username,
            Password = password
        };

        return SkillResult.SuccessResult(resultData,
            $"User '{firstName} {lastName}' ({email}) was successfully created. UserId: '{user.Id}', Username: '{username}', Password: '{password}'.");
    }

    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "@$!%*?&";

        var random = new Random();
        var password = new char[12];

        password[0] = upper[random.Next(upper.Length)];
        password[1] = lower[random.Next(lower.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];

        var allChars = upper + lower + digits + special;
        for (var i = 4; i < password.Length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        return new string(password.OrderBy(_ => random.Next()).ToArray());
    }
}
