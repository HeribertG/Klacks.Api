namespace Klacks.Api.Domain.Interfaces;

public interface ISettingsEncryptionService
{
    string Encrypt(string value);
    string Decrypt(string encryptedValue);
    bool IsSensitiveSettingType(string type);
    string ProcessForStorage(string type, string value);
    string ProcessForReading(string type, string value);
}
