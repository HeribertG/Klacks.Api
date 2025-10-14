using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs.Registrations;

namespace Klacks.Api.Domain.Interfaces;

public interface IAccountPasswordService
{
    Task<AuthenticatedResult> ChangePasswordAsync(ChangePasswordResource model);

    Task<AuthenticatedResult> ResetPasswordAsync(ResetPasswordResource data);

    Task<bool> GeneratePasswordResetTokenAsync(string email);

    Task<bool> ValidatePasswordResetTokenAsync(string token);
}