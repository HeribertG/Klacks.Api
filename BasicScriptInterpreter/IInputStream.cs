namespace Klacks.Api.BasicScriptInterpreter
{
    public interface IInputStream
    {
        int Col { get; }

        bool Eof { get; }

        InterpreterError ErrorObject { get; set; }

        int Index { get; }

        int Line { get; }

        IInputStream Connect(string connectString);

        string GetNextChar();

        void GoBack();

        void SkipComment();
    }
}
