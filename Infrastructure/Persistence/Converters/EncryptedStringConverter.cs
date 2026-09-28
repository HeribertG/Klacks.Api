// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces.Settings;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Klacks.Api.Infrastructure.Persistence.Converters;

public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(ISettingsEncryptionService encryptionService) : base(
        v => v == null ? null : encryptionService.Encrypt(v),
        v => v == null ? null : encryptionService.Decrypt(v))
    {
    }
}
