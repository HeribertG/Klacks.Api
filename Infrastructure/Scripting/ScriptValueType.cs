// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Scripting;

public enum ScriptValueType : byte
{
    Null = 0,
    Number = 1,
    Boolean = 2,
    String = 3,
    Object = 4
}
