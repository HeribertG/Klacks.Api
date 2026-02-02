# Backend Scripting Engine

## Übersicht

BASIC-ähnlicher Interpreter in C# für serverseitige Macro-Ausführung.

**Pfad:** `Infrastructure/Scripting/`

## Architektur

```
MacroEngine
    │
    ├── CompiledScript (einmal kompilieren, immutable)
    │
    └── ScriptExecutionContext (pro Ausführung)
```

## IMacroEngine Interface

```csharp
public interface IMacroEngine
{
    void PrepareMacro(Guid id, string script);
    void ResetImports();
    List<ResultMessage> Run(CancellationToken cancellationToken = default);
    dynamic? Imports { get; set; }
    void ImportItem(string key, object value);
    bool IsIde { get; set; }
    string ErrorCode { get; set; }
    int ErrorNumber { get; set; }
}
```

## Verwendung

```csharp
using var engine = new MacroEngine();

dynamic imports = new ExpandoObject();
imports.kundenName = "Max Mustermann";
imports.betrag = 100.50;
engine.Imports = imports;

engine.PrepareMacro(Guid.NewGuid(), @"
    Import kundenName
    Import betrag
    message 1, kundenName & "": "" & betrag & "" EUR""
");

if (engine.ErrorNumber == 0)
{
    var results = engine.Run();
}
```

## Sprachsyntax

### Variablen

```basic
DIM x
DIM a, b, c
CONST PI_VALUE = 3.14159

x = 10
x += 5
x &= " cm"  ' String-Konkatenation
```

### Operatoren

| Kategorie | Operatoren |
|-----------|------------|
| Arithmetik | `+`, `-`, `*`, `/`, `\` (Div), `%` (Mod), `^` (Power) |
| String | `&` (Konkatenation) |
| Vergleich | `=`, `<>`, `<`, `<=`, `>`, `>=` |
| Logik | `AND`, `OR`, `NOT` |

### Kontrollstrukturen

```basic
IF x > 10 THEN
    message 1, "groß"
ELSE
    message 1, "klein"
END IF

FOR i = 1 TO 10 STEP 2
    message 1, i
NEXT

DO WHILE x < 10
    x = x + 1
LOOP
```

### Funktionen

```basic
FUNCTION Add(a, b)
    Add = a + b
END FUNCTION

SUB PrintValue(v)
    message 1, v
END SUB
```

### Eingebaute Funktionen

| Funktion | Beschreibung |
|----------|--------------|
| `SIN(x)`, `COS(x)`, `TAN(x)` | Trigonometrie |
| `ATAN(x)` | Arcus Tangens |
| `IIF(cond, true, false)` | Inline-IF |
| `TimeToHours("HH:MM")` | Zeit zu Dezimal |
| `TimeOverlap(...)` | Zeitüberlappung |

### Konstanten

| Konstante | Wert |
|-----------|------|
| `PI` | 3.141592654 |
| `TRUE`, `FALSE` | Boolean |
| `VBCRLF`, `VBTAB` | Whitespace |

### I/O

```basic
MESSAGE 1, "Erfolg"   ' Type 1
MESSAGE 2, "Warnung"  ' Type 2
MESSAGE 3, "Fehler"   ' Type 3

DEBUGPRINT "Debug"
```

## Externe Variablen (Import)

```basic
Import kundenName
Import betrag

message 1, kundenName & ": " & betrag
```

## ScriptValue (Boxing-frei)

```csharp
public readonly struct ScriptValue
{
    public static ScriptValue FromNumber(double value);
    public static ScriptValue FromBoolean(bool value);
    public static ScriptValue FromString(string? value);

    public double AsDouble();
    public bool AsBoolean();
    public string AsString();
}
```

## Sicherheitsfeatures

- **Rekursionsschutz:** Max. 1000 Aufrufe
- **CancellationToken:** Kooperative Abbrüche
- **ScriptTooComplexException:** Bei Überschreitung

## Dateien

```
Infrastructure/Scripting/
├── MacroEngine.cs
├── IMacroEngine.cs
├── Code.cs
├── CompiledScript.cs
├── ScriptExecutionContext.cs
├── ScriptValue.cs
├── Opcodes.cs
├── SyntaxAnalyser.cs
│   ├── SyntaxAnalyser.Expressions.cs
│   ├── SyntaxAnalyser.Statements.cs
│   ├── SyntaxAnalyser.ControlFlow.cs
│   └── SyntaxAnalyser.BuiltIns.cs
└── Lexer/
```
