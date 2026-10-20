// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Persistence;

public interface IIdentityProviderSecretBackfill
{
    Task EncryptLegacySecretsAsync();
}
