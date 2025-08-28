using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Presentation.Services;

public interface IPasswordGeneratorService
{
    string GeneratePassword(int length = 12, bool includeUppercase = true, bool includeLowercase = true, 
        bool includeNumbers = true, bool includeSpecialChars = true);
}

public class PasswordGeneratorService : IPasswordGeneratorService
{
    private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    private const string NumberChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

    public string GeneratePassword(int length = 12, bool includeUppercase = true, bool includeLowercase = true, 
        bool includeNumbers = true, bool includeSpecialChars = true)
    {
        if (length < 8)
            throw new ArgumentException("Password length must be at least 8 characters.");

        var availableChars = new StringBuilder();
        var password = new StringBuilder();

        // Build character set based on requirements
        if (includeUppercase)
            availableChars.Append(UppercaseChars);
        
        if (includeLowercase)
            availableChars.Append(LowercaseChars);
        
        if (includeNumbers)
            availableChars.Append(NumberChars);
        
        if (includeSpecialChars)
            availableChars.Append(SpecialChars);

        if (availableChars.Length == 0)
            throw new ArgumentException("At least one character type must be selected.");

        // Ensure password contains at least one character from each required type
        var requiredChars = new List<char>();
        
        if (includeUppercase)
            requiredChars.Add(UppercaseChars[GetRandomIndex(UppercaseChars.Length)]);
        
        if (includeLowercase)
            requiredChars.Add(LowercaseChars[GetRandomIndex(LowercaseChars.Length)]);
        
        if (includeNumbers)
            requiredChars.Add(NumberChars[GetRandomIndex(NumberChars.Length)]);
        
        if (includeSpecialChars)
            requiredChars.Add(SpecialChars[GetRandomIndex(SpecialChars.Length)]);

        // Add required characters to password
        foreach (var requiredChar in requiredChars)
        {
            password.Append(requiredChar);
        }

        // Fill remaining length with random characters
        var remainingLength = length - requiredChars.Count;
        var allChars = availableChars.ToString();
        
        for (int i = 0; i < remainingLength; i++)
        {
            password.Append(allChars[GetRandomIndex(allChars.Length)]);
        }

        // Shuffle the password to randomize position of required characters
        return ShuffleString(password.ToString());
    }

    private static int GetRandomIndex(int maxValue)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0);
        return (int)(value % maxValue);
    }

    private static string ShuffleString(string input)
    {
        var array = input.ToCharArray();
        
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = GetRandomIndex(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
        
        return new string(array);
    }
}