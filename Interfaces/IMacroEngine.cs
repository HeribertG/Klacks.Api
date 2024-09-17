using Klacks_api.BasicScriptInterpreter;

namespace Klacks_api.Interfaces;

public interface IMacroEngine
{
  void PrepareMacro(Guid id, string script);
  void ResetImports();
  List<ResultMessage> Run();
  dynamic Imports { get; set; }
  void ImportItem(string key, object value);
  bool IsIde { get; set; }
  string ErrorCode { get; set; }
  int ErrorNumber { get; set; }
}
