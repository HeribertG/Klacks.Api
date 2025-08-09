namespace Klacks.Api.Presentation.Resources.Registrations;

public class UserResource
{
    public string Id { get; set; } = string.Empty;
     
    public string UserName { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
   
    public string Email { get; set; } = string.Empty;
    
    public bool IsAdmin { get; set; }
    
    public bool IsAuthorised { get; set; }
}
