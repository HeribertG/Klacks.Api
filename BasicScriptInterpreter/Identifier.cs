namespace Klacks.Api.BasicScriptInterpreter
{
    /// <summary>
    /// Identifier: Dient während Compile- und Laufzeit zur
    /// Aufnahme der relevanten Daten für benannte Variablen,
    /// Konstanten, Funktionen/Subs
    ///
    /// Insbesondere werden der Identifier-Typ, sein Wert - und
    /// bei Funktionen die Parameternamen gespeichert.
    ///
    /// Identifier-Objekte sind immer Elemente von Scope-Objekten.
    /// </summary>
    public class Identifier
    {
        public enum IdentifierTypes
        {
            IdIsVariableOfFunction = -2,
            IdSubOfFunction = -1,
            IdNone = 0,
            IdConst = 1,
            IdVariable = 2,
            IdFunction = 4,
            IdSub = 8,
        }

        public int Address { get; set; }

        public List<object>? FormalParameters { get; set; }

        public IdentifierTypes IdType { get; set; }

        public string? Name { get; set; }

        public object? Value { get; set; }

        // Adresse der Funktion

        // nur bei Funktionen: Namen der formalen Parameter
    }
}
