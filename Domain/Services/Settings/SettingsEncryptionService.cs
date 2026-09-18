// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Klacks.Api.Domain.Services.Settings;

public class SettingsEncryptionService : ISettingsEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<SettingsEncryptionService> _logger;

    private static readonly HashSet<string> SensitiveSettingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "outgoingserverPassword",
        "incomingServerPassword",
        "OPENROUTESERVICE_API_KEY",
        "DEEPL_API_KEY",
        "ASSISTANT_STT_API_KEY",
        "ASSISTANT_STT_API_KEY_DEEPGRAM",
        "ASSISTANT_STT_API_KEY_GROQ",
        "ASSISTANT_STT_API_KEY_ASSEMBLYAI",
        "ASSISTANT_TTS_API_KEY_OPENAI",
        "ASSISTANT_TTS_API_KEY_ELEVENLABS",
        "ASSISTANT_TTS_API_KEY_GOOGLE",
        "WEB_SEARCH_API_KEY"
    };

    private static readonly HashSet<string> ServerOnlySettingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "outgoingserverPassword",
        "incomingServerPassword",
        "OPENROUTESERVICE_API_KEY",
        "DEEPL_API_KEY",
        "ASSISTANT_STT_API_KEY",
        "ASSISTANT_STT_API_KEY_DEEPGRAM",
        "ASSISTANT_STT_API_KEY_GROQ",
        "ASSISTANT_STT_API_KEY_ASSEMBLYAI",
        "ASSISTANT_TTS_API_KEY_OPENAI",
        "ASSISTANT_TTS_API_KEY_ELEVENLABS",
        "ASSISTANT_TTS_API_KEY_GOOGLE",
        "WEB_SEARCH_API_KEY"
    };

    public bool IsServerOnlySettingType(string type)
    {
        return ServerOnlySettingTypes.Contains(type);
    }

    private const string EncryptedPrefix = "ENC:";

    private const string UndecryptableValueMessage =
        "Failed to decrypt an ENC:-prefixed setting value. The DataProtection key used to encrypt it is no longer in the key ring. Re-save the affected setting to re-encrypt it with the current key. Treating the value as not configured.";

    private const string DisposedDependencyMessage =
        "Failed to decrypt an ENC:-prefixed setting value because a DataProtection dependency was already disposed. This is an infrastructure failure, not a missing key, and must not be degraded to an unconfigured value.";

    public SettingsEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<SettingsEncryptionService> logger)
    {
        _protector = dataProtectionProvider.CreateProtector("Klacks.Settings.Encryption");
        _logger = logger;
    }

    public bool IsSensitiveSettingType(string type)
    {
        return SensitiveSettingTypes.Contains(type);
    }

    public string Encrypt(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            var encrypted = _protector.Protect(value);
            return $"{EncryptedPrefix}{encrypted}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt value");
            throw;
        }
    }

    public string Decrypt(string encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue))
        {
            return encryptedValue;
        }

        if (!encryptedValue.StartsWith(EncryptedPrefix))
        {
            return encryptedValue;
        }

        try
        {
            var cipherText = encryptedValue[EncryptedPrefix.Length..];
            return _protector.Unprotect(cipherText);
        }
        catch (Exception ex)
        {
            if (HasDisposedDependencyCause(ex))
            {
                _logger.LogError(ex, DisposedDependencyMessage);
                throw;
            }

            _logger.LogWarning(ex, UndecryptableValueMessage);
            return string.Empty;
        }
    }

    private static bool HasDisposedDependencyCause(Exception? exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is ObjectDisposedException)
            {
                return true;
            }
        }

        return false;
    }

    public string ProcessForStorage(string type, string value)
    {
        if (!IsSensitiveSettingType(type))
        {
            return value;
        }

        if (string.IsNullOrEmpty(value) || value.StartsWith(EncryptedPrefix))
        {
            return value;
        }

        _logger.LogDebug("Encrypting sensitive setting of type {Type}", type);
        return Encrypt(value);
    }

    public string ProcessForReading(string type, string value)
    {
        if (!IsSensitiveSettingType(type))
        {
            return value;
        }

        if (string.IsNullOrEmpty(value) || !value.StartsWith(EncryptedPrefix))
        {
            return value;
        }

        _logger.LogDebug("Decrypting sensitive setting of type {Type}", type);
        return Decrypt(value);
    }
}
