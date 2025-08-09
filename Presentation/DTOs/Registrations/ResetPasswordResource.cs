namespace Klacks.Api.Presentation.DTOs.Registrations
{
    public class ResetPasswordResource
    {
        public string Token { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
