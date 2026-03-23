// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for sending password change notification emails.
/// </summary>
/// <param name="result">Authentication result to attach mail success status to</param>
/// <param name="resource">Password change resource with email template and recipient</param>
using Klacks.Api.Domain.Interfaces.Accounts;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.DTOs.Registrations;

namespace Klacks.Api.Application.Helpers;

public static class PasswordChangeEmailHelper
{
    private const string MailFailure = "Email Send Failure";
    private const string TrueResult = "true";

    public static async Task<AuthenticatedResult> SendPasswordChangeEmailAsync(
        AuthenticatedResult result,
        ChangePasswordResource resource,
        IAccountNotificationService notificationService,
        IAccountAuthenticationService authService)
    {
        var message = resource.Message
            .Replace("{appName}", resource.AppName ?? "Klacks")
            .Replace("{password}", "********");

        var mailResult = await notificationService.SendEmailAsync(
            resource.Title,
            resource.Email,
            message);

        result.MailSuccess = false;

        if (!string.IsNullOrEmpty(mailResult))
        {
            result.MailSuccess = string.Compare(mailResult, TrueResult) == 0;
        }

        if (!result.MailSuccess)
        {
            authService.SetModelError(result, MailFailure, mailResult);
        }

        return result;
    }
}
