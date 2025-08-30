namespace Klacks.Api.Domain.Interfaces;

public interface IPasswordGeneratorService
{
    string GeneratePassword(int length = 12, bool includeUppercase = true, bool includeLowercase = true, 
        bool includeNumbers = true, bool includeSpecialChars = true);
}