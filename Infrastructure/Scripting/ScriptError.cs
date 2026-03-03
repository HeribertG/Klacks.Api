// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Scripting;

public sealed record ScriptError(int Code, string Description, int Line, int Column);
