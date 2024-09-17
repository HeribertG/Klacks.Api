namespace Klacks_api.Resources.Registrations
{
  public class TokenResource
  {
    public bool Success { get; set; }
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Name { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Id { get; set; } = "";

    public string ErrorMessage { get; set; } = "";
    public DateTime ExpTime { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsAuthorised { get; set; }
    public string Version { get; set; } = "";
  }
}
