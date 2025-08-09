
using Klacks.Api.Infrastructure.Interfaces;


namespace Klacks.Api.BasicScriptInterpreter
{
    public class MacroEngine : IDisposable, IMacroEngine
    {
        private Code code;
        private List<string> importList;
        private readonly Dictionary<Guid, ScriptCode> codeCollection;
        private readonly List<ResultMessage> result;

        public MacroEngine()
        {
            //Imports = new Imports();
            code = new Code();
            importList = new List<string>();
            codeCollection = new Dictionary<Guid, ScriptCode>();
            result = new List<ResultMessage>();
            AddEvent();
        }
#pragma warning disable CS8766 // Die NULL-Zulässigkeit von Verweistypen im Rückgabetyp entspricht (möglicherweise aufgrund von Attributen für die NULL-Zulässigkeit) nicht dem implizit implementierten Member.
        public dynamic? Imports { get; set; }
#pragma warning restore CS8766 // Die NULL-Zulässigkeit von Verweistypen im Rückgabetyp entspricht (möglicherweise aufgrund von Attributen für die NULL-Zulässigkeit) nicht dem implizit implementierten Member.
        public bool IsIde { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public int ErrorNumber { get; set; }

        public void ResetImports()
        {

            foreach (var item in importList)
            {
                SetImports(item);
            }
        }

        public void ImportItem(string key, object value)
        {
            code.ImportItem(key, value);
        }
        public void PrepareMacro(Guid id, string script)
        {


            //if (codeCollection.ContainsKey(id))
            //{

            //  PrepareImports(Script);
            //  var sc = codeCollection[id];

            //  code = sc.CurrentCode.Clone();
            //  ImportList = sc.ImportList;
            //}
            //else
            //{


            PrepareImports(script);

            code.Compile(script);
            ErrorNumber = code.ErrorObject!.Number;
            ErrorCode += code.ErrorObject.Description + "\n";

            //  ScriptCode sc = new ScriptCode();
            //  sc.CurrentCode = code;
            //  sc.ImportList = ImportList;
            //  sc.Script = Script;

            //  codeCollection.Add(id, sc);
            //}
        }
        public List<ResultMessage> Run()
        {
            result.Clear();
            code.ErrorObject!.Clear();
            ErrorNumber = 0;
            ErrorCode = "";
            code.Run();

            ErrorCode += code.ErrorObject.Description + "\n";

            return result;
        }

        #region code_events

        private void AddEvent()
        {
            code.Message += Code_Message;
            code.DebugClear += Code_DebugClear;
            code.DebugHide += Code_DebugHide;
            code.DebugShow += Code_DebugShow;
            code.DebugPrint += Code_DebugPrint;
        }

        private void RemoveEvent()
        {
            code.Message -= Code_Message;
            code.DebugClear -= Code_DebugClear;
            code.DebugHide -= Code_DebugHide;
            code.DebugShow -= Code_DebugShow;
            code.DebugPrint -= Code_DebugPrint;

        }

        private void Code_Message(int type, string message)
        {

            if (!string.IsNullOrEmpty(message))
            {
                if (type > 0)
                {
                    var c = new ResultMessage()
                    {
                        Type = type,
                        Message = message
                    };

                    result.Add(c);
                }

            }

        }


        private void Code_DebugClear()
        {
            ErrorCode = "";
        }

        private void Code_DebugHide()
        {

        }

        private void Code_DebugShow()
        {

        }

        private void Code_DebugPrint(string msg)
        {
            ErrorCode += msg + "\n";
        }

        #endregion code_events

        private void PrepareImports(string script)
        {
            var i = 1;
            int endposition;
            code.ImportClear();
            importList.Clear();

            script = RemoveDescription(script);

            while (!(i == -1))
            {
                i = script.IndexOf("Import");
                if (i < 0) { break; }


                if (!string.IsNullOrEmpty(script))
                {
                    endposition = script.IndexOf("\r\n", i + 1, StringComparison.Ordinal);
                    if (endposition == -1)
                        endposition = script.IndexOf("\r", i + 1, StringComparison.Ordinal);
                    if (endposition == -1)
                        endposition = script.IndexOf("\n", i + 1, StringComparison.Ordinal);

                    if (endposition < i)
                        break;

                    string tmpstr = script.Substring(i, endposition - i + 1);
                    script = script.Remove(i, endposition - i + 1);

                    tmpstr = tmpstr.Replace("\r\n", string.Empty);
                    tmpstr = tmpstr.Replace("\r", string.Empty);
                    tmpstr = tmpstr.Replace("\n", string.Empty);
                    tmpstr = tmpstr.Replace("Import", string.Empty);
                    tmpstr = tmpstr.Trim();
                    i = 0;

                    SetImports(tmpstr);
                }
            }


        }

        private string RemoveDescription(string script)
        {
            int endposition;
            var i = 1;
            while (i != -1)
            {
                i = script.IndexOf("'");
                if (i < 0) { break; }


                if (!string.IsNullOrEmpty(script))
                {
                    endposition = script.IndexOf("\r\n", i + 1, StringComparison.Ordinal);
                    if (endposition == -1)
                        endposition = script.IndexOf("\r", i + 1, StringComparison.Ordinal);
                    if (endposition == -1)
                        endposition = script.IndexOf("\n", i + 1, StringComparison.Ordinal);

                    if (endposition < i) { break; }


                    script = script.Remove(i, endposition - i + 1);


                    i = 0;

                }
            }


            return script;

        }
        private void SetImports(string key)
        {
            try
            {
                var res = ReadImports(key);

                code.ImportAdd(key, res);
            }
            catch (Exception)
            {
                code.ImportAdd(key, 0);
            }

        }

        private object ReadImports(string key)
        {
            IDictionary<string, object> propertyValues = (IDictionary<string, object>)Imports!;
            var info = propertyValues![key];

            return info;
        }

        public void Dispose()
        {
            RemoveEvent();
        }

        ~MacroEngine()
        {
            Dispose();
        }
    }

    public class ResultMessage
    {
        public int Type { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
