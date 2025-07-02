namespace Klacks.Api.Interfaces;

public interface IUserService
{
    Guid? GetId();

    string? GetIdString();

    string GetUserName();

    Task<bool> IsAdmin();
}
