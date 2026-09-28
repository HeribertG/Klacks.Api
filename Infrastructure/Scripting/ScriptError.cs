// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Scripting;

public sealed record ScriptError(int Code, string Description, int Line, int Column);
