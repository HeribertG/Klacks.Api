// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Keys of the metadata dictionary of a skill result that more than one layer reads or writes.
/// </summary>
public static class SkillResultMetadataKeys
{
    /// <summary>The one-time token of a confirmation result; written by SkillResult.Confirmation, read by the chat bridge and the MCP handler.</summary>
    public const string ConfirmationToken = "confirmationToken";
}
