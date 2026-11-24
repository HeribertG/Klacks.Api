// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Interfaces;

namespace Klacks.Api.Infrastructure.Scripting;

public class MacroEngine : IDisposable, IMacroEngine
{
    private ScriptExecutionContext? context;
    private readonly List<ResultMessage> result = new();

    public bool IsIde { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public int ErrorNumber { get; set; }

    public List<ResultMessage> RunWithScript(CompiledScript script, CancellationToken cancellationToken = default)
    {
        result.Clear();
        ErrorNumber = 0;
        ErrorCode = string.Empty;

        if (script == null || script.HasError)
        {
            return result;
        }

        context = new ScriptExecutionContext(script);
        context.AllowUi = IsIde;

        context.Message += Context_Message;
        context.DebugPrint += Context_DebugPrint;
        context.DebugClear += Context_DebugClear;
        context.DebugShow += Context_DebugShow;
        context.DebugHide += Context_DebugHide;

        try
        {
            var runResult = context.Execute(cancellationToken);

            if (!runResult.Success && runResult.Error != null)
            {
                ErrorNumber = runResult.Error.Code;
                ErrorCode = runResult.Error.Description + "\n";
            }

            result.Clear();
            result.AddRange(runResult.Messages);
        }
        finally
        {
            context.Message -= Context_Message;
            context.DebugPrint -= Context_DebugPrint;
            context.DebugClear -= Context_DebugClear;
            context.DebugShow -= Context_DebugShow;
            context.DebugHide -= Context_DebugHide;
        }

        return result;
    }

    public void Dispose()
    {
    }

    #region Events

    private void Context_Message(int type, string message)
    {
        if (!string.IsNullOrEmpty(message) && type > 0)
        {
            result.Add(new ResultMessage { Type = type, Message = message });
        }
    }

    private void Context_DebugClear()
    {
        ErrorCode = string.Empty;
    }

    private void Context_DebugHide()
    {
    }

    private void Context_DebugShow()
    {
    }

    private void Context_DebugPrint(string msg)
    {
        ErrorCode += msg + "\n";
    }

    #endregion Events
}
