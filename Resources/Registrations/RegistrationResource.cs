namespace Klacks.Api.Resources.Registrations
{
    public class RegistrationResource
    {
        public string AppName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool SendEmail { get; set; } = false;

        public string Title { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
    }
}
