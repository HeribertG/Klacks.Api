// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates another person's login account. The target is resolved by userId, or by an unambiguous
/// match on user name or email address; an ambiguous name is answered with the candidates rather
/// than picking one. The write is read back afterwards and reported as unpersisted on mismatch.
/// Roles are not touched here.
/// </summary>
/// <param name="userId">Id of the account to update; alternative to userName.</param>
/// <param name="userName">User name or email address identifying the account; alternative to userId.</param>
/// <param name="firstName">Optional new first name.</param>
/// <param name="lastName">Optional new last name.</param>
/// <param name="email">Optional new email address.</param>
/// <param name="newUserName">Optional new user name.</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_user")]
public class UpdateUserSkill : BaseSkillImplementation
{
    private const int MaxListedCandidates = 10;

    private readonly IMediator _mediator;

    public UpdateUserSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var users = await _mediator.Send(new GetUserListQuery(), cancellationToken);

        var userId = GetParameter<string>(parameters, "userId")?.Trim();
        var userName = GetParameter<string>(parameters, "userName")?.Trim();

        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(userName))
        {
            return SkillResult.Error("Either 'userId' or 'userName' is required to identify the account.");
        }

        UserResource? account;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            account = users.FirstOrDefault(u => string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));
            if (account == null)
            {
                return SkillResult.Error($"No login account with id '{userId}'.");
            }
        }
        else
        {
            var matches = users
                .Where(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(u.Email, userName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                return SkillResult.Error(
                    $"No login account matches '{userName}'. Available accounts: " +
                    string.Join(", ", users.Take(MaxListedCandidates).Select(u => u.UserName)));
            }

            if (matches.Count > 1)
            {
                return SkillResult.Error(
                    $"'{userName}' matches {matches.Count} accounts: " +
                    string.Join(", ", matches.Select(u => $"{u.UserName} ({u.Email})")) +
                    ". Repeat the call with userId.");
            }

            account = matches[0];
        }

        var changed = new List<string>();

        var firstName = GetParameter<string>(parameters, "firstName");
        if (!string.IsNullOrWhiteSpace(firstName) && firstName.Trim() != account.FirstName)
        {
            account.FirstName = firstName.Trim();
            changed.Add("firstName");
        }

        var lastName = GetParameter<string>(parameters, "lastName");
        if (!string.IsNullOrWhiteSpace(lastName) && lastName.Trim() != account.LastName)
        {
            account.LastName = lastName.Trim();
            changed.Add("lastName");
        }

        var email = GetParameter<string>(parameters, "email");
        if (!string.IsNullOrWhiteSpace(email) && email.Trim() != account.Email)
        {
            if (!email.Contains('@'))
            {
                return SkillResult.Error($"Invalid email address: {email}");
            }

            account.Email = email.Trim();
            changed.Add("email");
        }

        var newUserName = GetParameter<string>(parameters, "newUserName");
        if (!string.IsNullOrWhiteSpace(newUserName) && newUserName.Trim() != account.UserName)
        {
            account.UserName = newUserName.Trim();
            changed.Add("userName");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { AccountId = account.Id, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — the account was left unchanged.");
        }

        var update = new UpdateAccountResource
        {
            Id = Guid.Parse(account.Id),
            FirstName = account.FirstName,
            LastName = account.LastName,
            Email = account.Email,
            UserName = account.UserName
        };

        var result = await _mediator.Send(new UpdateAccountCommand(update), cancellationToken);
        if (result is not { Success: true })
        {
            return SkillResult.Error($"Updating the account failed: {result?.Messages ?? "unknown error"}");
        }

        var reloaded = await _mediator.Send(new GetUserListQuery(), cancellationToken);
        var persisted = reloaded.FirstOrDefault(u => string.Equals(u.Id, account.Id, StringComparison.OrdinalIgnoreCase));

        if (persisted == null
            || persisted.FirstName != account.FirstName
            || persisted.LastName != account.LastName
            || persisted.Email != account.Email
            || persisted.UserName != account.UserName)
        {
            return SkillResult.Error(
                "Database verification failed: the account does not show the new values after the write — " +
                "treat the update as not persisted.");
        }

        return SkillResult.SuccessResult(
            new
            {
                AccountId = account.Id,
                ChangedFields = changed,
                persisted.FirstName,
                persisted.LastName,
                persisted.Email,
                persisted.UserName
            },
            $"Account '{persisted.UserName}' updated ({string.Join(", ", changed)}) and confirmed in the database (verified).");
    }
}
