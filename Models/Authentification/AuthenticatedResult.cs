using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Klacks.Api.Models.Authentification;

public class AuthenticatedResult
{
  public string Email { get; set; } = string.Empty;

  public DateTime Expires { get; set; }

  public string FirstName { get; set; } = string.Empty;

  public string Id { get; set; } = string.Empty;

  public bool IsAdmin { get; set; }

  public bool IsAuthorised { get; set; }

  public bool MailSuccess { get; set; }

  public string Message { get; set; } = string.Empty;

  public ModelStateDictionary? ModelState { get; set; }

  public string Name { get; set; } = string.Empty;

  public string PasswordResetToken { get; set; } = string.Empty;

  public string RefreshToken { get; set; } = string.Empty;

  public bool Success { get; set; }

  public string Token { get; set; } = string.Empty;

  public string UserName { get; set; } = string.Empty;
}
