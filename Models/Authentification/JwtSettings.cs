namespace Klacks_api.Models.Authentification;

public class JwtSettings
{
  public string Secret { get; set; } = string.Empty;

  public string ValidAudience { get; set; } = string.Empty;

  public string ValidIssuer { get; set; } = string.Empty;
}
