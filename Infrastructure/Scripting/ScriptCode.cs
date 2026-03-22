// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Scripting
{
    /// <summary>
    /// Used to reuse compiled code repeatedly without recompilation.
    /// Script contains all external variable names.
    /// Code is a clone of the compiled code class.
    /// </summary>
    internal class ScriptCode
    {
        public Code? CurrentCode { get; set; }

        public List<string>? ImportList { get; set; }

        public string Script { get; set; } = string.Empty;
    }
}
