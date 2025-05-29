namespace Klacks.Api.Interfaces;

public interface IUserService
{
    Guid? GetId();
    string GetUserName();
}
