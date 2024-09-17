namespace Klacks_api.Resources
{
  public class RegistrationResource
  {
    public string UserName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool SendEmail { get; set; } = false;

  }
}
