using Klacks.Api.Models.Authentification;
using Klacks.Api.Resources;
using Klacks.Api.Resources.Registrations;

namespace Klacks.Api.Interfaces;

public interface IAccountRepository
{
  Task<AuthenticatedResult> ChangePassword(ChangePasswordResource model);

  Task<AuthenticatedResult> ChangePasswordUser(ChangePasswordResource model);

  Task<HttpResultResource> ChangeRoleUser(ChangeRole editUserRole);

  Task<HttpResultResource> DeleteAccountUser(Guid id);

  Task<List<UserResource>> GetUserList();

  Task<AuthenticatedResult> LogInUser(string email, string password);

  Task<AuthenticatedResult> RefreshToken(RefreshRequestResource model);

  Task<AuthenticatedResult> RegisterUser(AppUser user, string password);

  Task<AuthenticatedResult> ResetPassword(ResetPasswordResource data);

  Task<string> SendEmail(string title, string email, string message);

  void SetModelError(AuthenticatedResult model, string key, string message);
}
