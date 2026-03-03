// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IAuthenticationService
{
    Task<(bool IsValid, AppUser? User)> ValidateCredentialsAsync(string email, string password);

    Task<(bool Success, IdentityResult? Result)> ChangePasswordAsync(AppUser user, string oldPassword, string newPassword);

    Task<AppUser?> GetUserFromAccessTokenAsync(string token);

    ModelStateDictionary AddErrorsToModelState(IdentityResult identityResult, ModelStateDictionary? modelState = null);

    void SetModelError(AuthenticatedResult model, string key, string message);

    Task<(bool Success, IdentityResult? Result)> ResetPasswordAsync(AppUser user, string token, string newPassword);
}