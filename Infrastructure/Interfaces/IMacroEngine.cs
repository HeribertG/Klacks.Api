using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Interfaces;

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
