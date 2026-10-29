// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// JSON schema keys of a skill parameter that are written by the toolset builders and read back by the chat loop,
/// so producer and consumer share one spelling.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SkillParameterSchemaKeys
{
    public const string Enum = "enum";
}
