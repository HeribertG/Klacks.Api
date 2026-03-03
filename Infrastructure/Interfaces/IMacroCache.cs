// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Interfaces;

public interface IMacroCache
{
    CompiledScript GetOrCompile(Guid macroId, string script);
    void Invalidate(Guid macroId);
    void InvalidateAll();
    bool Contains(Guid macroId);
}
