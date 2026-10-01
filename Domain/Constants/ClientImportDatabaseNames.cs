// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Database object names of the employee import that code has to recognize: the commit maps only a
/// unique violation of the token index to "already committed".
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClientImportDatabaseNames
{
    public const string TokenIndex = "ix_client_import_batches_token";
}
