// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Interfaces;

namespace Klacks.Api.Infrastructure.Scripting;

public class MacroEngine : IDisposable, IMacroEngine
{
    private CompiledScript? compiledScript;
    private ScriptExecutionContext? context;
    private List<string> importList = new();
    private readonly List<ResultMessage> result = new();

    public dynamic? Imports { get; set; }
    public bool IsIde { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public int ErrorNumber { get; set; }

    public void PrepareMacro(Guid id, string script)
    {
        ExtractImportKeys(script);

        compiledScript = CompiledScript.Compile(script);

        if (compiledScript.HasError)
        {
            ErrorNumber = compiledScript.Error!.Code;
            ErrorCode = compiledScript.Error.Description + "\n";
        }
        else
        {
            ErrorNumber = 0;
            ErrorCode = string.Empty;
            SetImportValues();
        }
    }

    public List<ResultMessage> Run(CancellationToken cancellationToken = default)
    {
        if (compiledScript == null || compiledScript.HasError)
        {
            result.Clear();
            return result;
        }

        return RunWithScript(compiledScript, cancellationToken);
    }

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

            foreach (var msg in runResult.Messages)
            {
                if (!result.Contains(msg))
                {
                    result.Add(msg);
                }
            }
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

    public void ResetImports()
    {
        SetImportValues();
    }

    public void ImportItem(string key, object value)
    {
        compiledScript?.SetExternalValue(key, value);
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

    #region Import Handling

    private void ExtractImportKeys(string script)
    {
        importList.Clear();
        script = RemoveComments(script);

        int i = 0;
        while (i != -1)
        {
            i = script.IndexOf("Import", StringComparison.OrdinalIgnoreCase);
            if (i < 0) break;

            int endposition = FindLineEnd(script, i + 1);
            if (endposition < i) break;

            string line = script.Substring(i, endposition - i + 1);
            script = script.Remove(i, endposition - i + 1);

            string key = line
                .Replace("\r\n", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("Import", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (!string.IsNullOrEmpty(key))
            {
                importList.Add(key);
            }

            i = 0;
        }
    }

    private static string RemoveComments(string script)
    {
        int i = 0;
        while (i != -1)
        {
            i = script.IndexOf('\'');
            if (i < 0) break;

            int endposition = FindLineEnd(script, i + 1);
            if (endposition < i) break;

            script = script.Remove(i, endposition - i + 1);
            i = 0;
        }

        return script;
    }

    private static int FindLineEnd(string script, int startIndex)
    {
        int endposition = script.IndexOf("\r\n", startIndex, StringComparison.Ordinal);
        if (endposition == -1)
            endposition = script.IndexOf('\r', startIndex);
        if (endposition == -1)
            endposition = script.IndexOf('\n', startIndex);
        return endposition;
    }

    private void SetImportValues()
    {
        if (compiledScript == null || Imports == null) return;

        IDictionary<string, object> propertyValues = (IDictionary<string, object>)Imports;

        foreach (var key in importList)
        {
            try
            {
                if (propertyValues.TryGetValue(key, out var value))
                {
                    compiledScript.SetExternalValue(key, value);
                }
                else
                {
                    compiledScript.SetExternalValue(key, 0);
                }
            }
            catch
            {
                compiledScript.SetExternalValue(key, 0);
            }
        }
    }

    #endregion Import Handling
}

public class ResultMessage
{
    public int Type { get; set; }
    public string Message { get; set; } = string.Empty;
}
