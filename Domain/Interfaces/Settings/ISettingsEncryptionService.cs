// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ISettingsEncryptionService
{
    string Encrypt(string value);
    string Decrypt(string encryptedValue);
    bool IsSensitiveSettingType(string type);
    bool IsServerOnlySettingType(string type);
    string ProcessForStorage(string type, string value);
    string ProcessForReading(string type, string value);
}
