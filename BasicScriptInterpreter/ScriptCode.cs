namespace Klacks.Api.BasicScriptInterpreter
{
  /// <summary>
  /// Wird gebraucht, um compilierte Code immer wieder zu gebrauchen ohne erneute Compilierung
  /// Script enthält alle externen Variablennamen
  /// Code ein Clone der  compilierten Code Klasse.
  /// </summary>
  internal class ScriptCode
  {
    public Code? CurrentCode { get; set; }

    public List<string>? ImportList { get; set; }

    public string Script { get; set; } = string.Empty;
  }
}
