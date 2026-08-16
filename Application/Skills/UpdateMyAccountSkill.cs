// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates the requesting user's OWN login account (display first/last name, email address,
/// login user name). Strictly self-scoped: the account is always resolved from the execution
/// context's user id — other users' accounts cannot be touched (use the user-administration
/// skills for that). Only supplied fields change; the update is verified by re-reading the
/// account afterwards. Passwords are deliberately out of scope for the assistant.
/// </summary>
/// <param name="firstName">Optional. New first name.</param>
/// <param name="lastName">Optional. New last name.</param>
/// <param name="email">Optional. New email address of the login account.</param>
/// <param name="userName">Optional. New login user name.</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_my_account")]
public class UpdateMyAccountSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateMyAccountSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var account = await LoadOwnAccountAsync(context, cancellationToken);
        if (account == null)
        {
            return SkillResult.Error("Your own login account could not be loaded.");
        }

        var changed = new List<string>();

        var firstName = GetParameter<string>(parameters, "firstName");
        if (!string.IsNullOrWhiteSpace(firstName) && firstName != account.FirstName)
        {
            account.FirstName = firstName;
            changed.Add("firstName");
        }

        var lastName = GetParameter<string>(parameters, "lastName");
        if (!string.IsNullOrWhiteSpace(lastName) && lastName != account.LastName)
        {
            account.LastName = lastName;
            changed.Add("lastName");
        }

        var email = GetParameter<string>(parameters, "email");
        if (!string.IsNullOrWhiteSpace(email) && email != account.Email)
        {
            if (!email.Contains('@'))
            {
                return SkillResult.Error($"Invalid email address: {email}");
            }

            account.Email = email;
            changed.Add("email");
        }

        var userName = GetParameter<string>(parameters, "userName");
        if (!string.IsNullOrWhiteSpace(userName) && userName != account.UserName)
        {
            account.UserName = userName;
            changed.Add("userName");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { AccountId = account.Id, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — your account was left unchanged.");
        }

        var update = new UpdateAccountResource
        {
            Id = Guid.Parse(account.Id),
            FirstName = account.FirstName,
            LastName = account.LastName,
            Email = account.Email,
            UserName = account.UserName,
            PhoneNumber = account.PhoneNumber
        };

        var result = await _mediator.Send(new UpdateAccountCommand(update), cancellationToken);
        if (result is not { Success: true })
        {
            return SkillResult.Error(
                $"Updating your account failed: {result?.Messages ?? "unknown error"}");
        }

        var persisted = await LoadOwnAccountAsync(context, cancellationToken);
        if (persisted == null
            || persisted.FirstName != account.FirstName
            || persisted.LastName != account.LastName
            || persisted.Email != account.Email
            || persisted.UserName != account.UserName)
        {
            return SkillResult.Error(
                "Database verification failed: your account does not show the new values after the write — " +
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
            $"Your account was updated ({string.Join(", ", changed)}) and confirmed in the database (verified).");
    }

    private async Task<UserResource?> LoadOwnAccountAsync(
        SkillExecutionContext context, CancellationToken cancellationToken)
    {
        var users = await _mediator.Send(new GetUserListQuery(), cancellationToken);
        var ownId = context.UserId.ToString();
        return users.FirstOrDefault(u => string.Equals(u.Id, ownId, StringComparison.OrdinalIgnoreCase));
    }
}
