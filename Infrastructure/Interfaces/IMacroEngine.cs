// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Interfaces;

public interface IMacroEngine
{
    List<ResultMessage> RunWithScript(CompiledScript script, CancellationToken cancellationToken = default);
    bool IsIde { get; set; }
    string ErrorCode { get; set; }
    int ErrorNumber { get; set; }
}
