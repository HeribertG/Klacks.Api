# Backend Scripting Macro Engine - Dokumentation

## Übersicht

Die Backend Scripting Macro Engine ist ein vollständiger BASIC-ähnlicher Interpreter, implementiert in C#. Sie ermöglicht die serverseitige Ausführung von Makro-Skripten in der .NET API.

**Pfad:** `Infrastructure/Scripting/`

## Architektur

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           MacroEngine                                    │
│                    (IMacroEngine Implementation)                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
          ┌─────────────┐  ┌──────────────┐  ┌──────────────┐
          │   Lexer     │  │    Parser    │  │   Executor   │
          │ (Tokenizer) │  │  (Compiler)  │  │ (Interpreter)│
          └─────────────┘  └──────────────┘  └──────────────┘
                │                  │                │
                ▼                  ▼                ▼
          StringInputStream   SyntaxAnalyser       Code
                             (Partial Classes)  (Bytecode)
                                                    │
                                    ┌───────────────┼───────────────┐
                                    ▼               ▼               ▼
                            CompiledScript  ScriptExecution   ScriptValue
                            (Immutable)       Context         (Value Type)
```

### Erweiterte Architektur (Thread-Safe Ausführung)

Für thread-safe Mehrfachausführung eines kompilierten Skripts:

```
┌──────────────┐        ┌───────────────────┐        ┌─────────────────┐
│    Source    │───────▶│  CompiledScript   │───────▶│ ScriptExecution │
│   (String)   │        │   (Immutable)     │        │    Context      │
└──────────────┘        └───────────────────┘        └─────────────────┘
                               │                             │
                               │   Einmal kompilieren        │   Pro Ausführung
                               │                             │
                               ▼                             ▼
                        ┌──────────────┐              ┌──────────────┐
                        │ Instructions │              │ ScriptResult │
                        │ External Vars│              │ (Success/Err)│
                        └──────────────┘              └──────────────┘
```

## Komponenten

### 1. MacroEngine (`MacroEngine.cs`)

Der Haupteinstiegspunkt für die Skriptausführung. Implementiert `IMacroEngine`.

```csharp
public class MacroEngine : IDisposable, IMacroEngine
{
    public dynamic? Imports { get; set; }
    public bool IsIde { get; set; }
    public string ErrorCode { get; set; }
    public int ErrorNumber { get; set; }

    public void PrepareMacro(Guid id, string script);
    public void ImportItem(string key, object value);
    public List<ResultMessage> Run();
    public void ResetImports();
}
```

**Verwendung:**
```csharp
using var engine = new MacroEngine();

// Externe Variablen setzen
dynamic imports = new ExpandoObject();
imports.kundenName = "Max Mustermann";
imports.betrag = 100.50;
engine.Imports = imports;

// Kompilieren
engine.PrepareMacro(Guid.NewGuid(), @"
    Import kundenName
    Import betrag
    message 1, kundenName & "": "" & betrag & "" EUR""
");

if (engine.ErrorNumber == 0)
{
    // Ausführen
    var results = engine.Run();
    foreach (var result in results)
    {
        Console.WriteLine($"[{result.Type}] {result.Message}");
    }
}
```

### 2. IMacroEngine Interface (`IMacroEngine.cs`)

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

**CancellationToken-Unterstützung:** Die `Run()`-Methode unterstützt kooperative Abbrüche via CancellationToken.

### 3. Code-Klasse (`Code.cs`)

Verwaltet Kompilierung, Bytecode und Ausführung.

```csharp
public class Code : IDisposable
{
    // Events für I/O
    public event MessageEventHandler? Message;
    public event DebugPrintEventHandler? DebugPrint;
    public event DebugClearEventHandler? DebugClear;
    public event DebugShowEventHandler? DebugShow;
    public event DebugHideEventHandler? DebugHide;
    public event AssignEventHandler? Assign;
    public event RetrieveEventHandler? Retrieve;

    // Eigenschaften
    public bool AllowUi { get; set; }
    public bool Cancel { get; set; }
    public int CodeTimeout { get; set; } = 120000;  // 2 Minuten
    public InterpreterError? ErrorObject { get; }
    public bool Running { get; }

    // Methoden
    public bool Compile(string source, bool optionExplicit = true, bool allowExternal = true);
    public bool Run(CancellationToken cancellationToken = default);
    public Code Clone();
    public Identifier ImportAdd(string name, ScriptValue value = default, IdentifierTypes idType = IdVariable);
    public void ImportItem(string name, ScriptValue value);
    public ScriptValue ImportRead(string name);
    public void ImportClear();
}
```

**Sicherheitsfeatures:**
- **Rekursionsschutz:** Maximale Rekursionstiefe von 1000 Aufrufen
- **CancellationToken:** Kooperative Abbrüche während der Ausführung
- **ScriptTooComplexException:** Wird bei Überschreitung der Rekursionstiefe geworfen

### 4. Syntax Analyse (Parser)

Der Parser verwendet C# **Partial Classes** für bessere Wartbarkeit:

```
SyntaxAnalyser.cs              (Hauptklasse mit Parse())
    │
    ├── SyntaxAnalyser.Expressions.cs   (Ausdrücke, Bedingungen)
    ├── SyntaxAnalyser.Statements.cs    (Zuweisungen, Deklarationen)
    ├── SyntaxAnalyser.ControlFlow.cs   (IF, FOR, DO)
    └── SyntaxAnalyser.BuiltIns.cs      (MsgBox, InputBox, Message)
```

### 5. Opcodes (`Opcodes.cs`)

```csharp
public enum Opcodes
{
    // Speicher
    AllocConst = 0,      AllocVar = 1,
    PushValue = 2,       PushVariable = 3,
    Pop = 4,             PopWithIndex = 5,
    Assign = 6,

    // Arithmetik
    Add = 7,             Sub = 8,
    Multiplication = 9,  Division = 10,
    Div = 11,            Mod = 12,
    Power = 13,          StringConcat = 14,

    // Logik
    Or = 15,             And = 16,

    // Vergleich
    Eq = 17,             NotEq = 18,
    Lt = 19,             LEq = 20,
    Gt = 21,             GEq = 22,

    // Unär
    Negate = 23,         Not = 24,
    Factorial = 25,

    // Trigonometrie
    Sin = 26,            Cos = 27,
    Tan = 28,            ATan = 29,

    // I/O
    DebugPrint = 30,     DebugClear = 31,
    DebugShow = 32,      DebugHide = 33,
    Msgbox = 34,         DoEvents = 35,
    Inputbox = 36,       Message = 45,

    // Kontrolle
    Jump = 37,           JumpTrue = 38,
    JumpFalse = 39,      JumpPop = 40,

    // Scopes
    PushScope = 41,      PopScope = 42,
    Call = 43,           Return = 44
}
```

### 6. Identifier (`Identifier.cs`)

```csharp
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
        IdSub = 8
    }

    public string? Name { get; set; }
    public ScriptValue Value { get; set; }  // Boxing-freier Werttyp
    public IdentifierTypes IdType { get; set; }
    public int Address { get; set; }
    public List<object>? FormalParameters { get; set; }
}
```

### 7. ScriptValue (`ScriptValue.cs`)

Ein struct zur Boxing-freien Speicherung von Skript-Werten:

```csharp
public enum ScriptValueType : byte
{
    Null = 0,
    Number = 1,
    Boolean = 2,
    String = 3,
    Object = 4
}

public readonly struct ScriptValue : IEquatable<ScriptValue>
{
    // Factory-Methoden
    public static ScriptValue Null { get; }
    public static ScriptValue FromNumber(double value);
    public static ScriptValue FromInt(int value);
    public static ScriptValue FromBoolean(bool value);
    public static ScriptValue FromString(string? value);
    public static ScriptValue FromObject(object? value);

    // Typ-Prüfung
    public ScriptValueType Type { get; }
    public bool IsNull { get; }
    public bool IsNumber { get; }
    public bool IsBoolean { get; }
    public bool IsString { get; }
    public bool IsObject { get; }

    // Wert-Extraktion
    public double AsDouble();
    public int AsInt();
    public bool AsBoolean();
    public string AsString();  // Infinity wird als ∞/-∞ formatiert
    public object? AsObject();

    // Implizite Konvertierungen
    public static implicit operator ScriptValue(double value);
    public static implicit operator ScriptValue(int value);
    public static implicit operator ScriptValue(bool value);
    public static implicit operator ScriptValue(string? value);

    // Operatoren
    public static ScriptValue operator +(ScriptValue left, ScriptValue right);
    public static ScriptValue operator -(ScriptValue left, ScriptValue right);
    public static ScriptValue operator *(ScriptValue left, ScriptValue right);
    public static ScriptValue operator /(ScriptValue left, ScriptValue right);
    public static ScriptValue operator %(ScriptValue left, ScriptValue right);
    public static ScriptValue operator -(ScriptValue value);
    public static ScriptValue operator !(ScriptValue value);
}
```

**Vorteile:**
- Primitive Typen (double, int, bool) werden ohne Heap-Allokation gespeichert
- Keine Boxing/Unboxing-Kosten bei Berechnungen
- Type-safe Wertverarbeitung
- Infinity-Formatierung eingebaut (∞/-∞)

### 8. CompiledScript (`CompiledScript.cs`)

Repräsentiert ein einmal kompiliertes, unveränderliches Skript:

```csharp
public sealed class CompiledScript
{
    public IReadOnlyList<object[]> Instructions { get; }
    public IReadOnlyDictionary<string, Identifier> ExternalSymbols { get; }
    public bool HasError { get; }
    public ScriptError? Error { get; }

    public static CompiledScript Compile(string source, bool optionExplicit = true);
    public void SetExternalValue(string name, object? value);
}
```

### 9. ScriptExecutionContext (`ScriptExecutionContext.cs`)

Führt ein CompiledScript aus (für thread-safe Mehrfachausführung):

```csharp
public sealed class ScriptExecutionContext
{
    public event Code.MessageEventHandler? Message;
    public event Code.DebugPrintEventHandler? DebugPrint;
    public bool AllowUi { get; set; }

    public ScriptExecutionContext(CompiledScript script);
    public ScriptResult Execute(CancellationToken cancellationToken = default);
}
```

### 10. ScriptResult und ScriptError

```csharp
public sealed record ScriptResult
{
    public bool Success { get; init; }
    public List<ResultMessage> Messages { get; init; }
    public ScriptError? Error { get; init; }

    public static ScriptResult Ok(List<ResultMessage>? messages = null);
    public static ScriptResult Fail(ScriptError error, List<ResultMessage>? messages = null);
}

public sealed record ScriptError(int Code, string Description, int Line, int Column);
```

## Sprachsyntax

Die Syntax ist identisch zur Frontend-Engine (BASIC-ähnlich).

### Variablen und Konstanten

```basic
' Variablendeklaration
DIM x
DIM a, b, c

' Konstantendeklaration
CONST PI_VALUE = 3.14159
CONST GREETING = "Hallo"

' Zuweisung
x = 10
x += 5      ' x = x + 5
x -= 3      ' x = x - 3
x *= 2      ' x = x * 2
x /= 4      ' x = x / 4
x &= " cm"  ' String-Konkatenation
```

### Operatoren

| Kategorie | Operatoren |
|-----------|------------|
| Arithmetik | `+`, `-`, `*`, `/`, `\` (Div), `%` (Mod), `^` (Power), `!` (Fakultät) |
| String | `&` (Konkatenation) |
| Vergleich | `=`, `<>`, `<`, `<=`, `>`, `>=` |
| Logik | `AND`, `OR`, `NOT` |
| Zuweisung | `+=`, `-=`, `*=`, `/=`, `&=`, `\=`, `%=` |

### Kontrollstrukturen

```basic
' IF-Statement
IF x > 10 THEN
    message 1, "groß"
ELSE
    message 1, "klein"
END IF

' FOR-Schleife
FOR i = 1 TO 10 STEP 2
    message 1, i
NEXT

' DO-WHILE
DO WHILE x < 10
    x = x + 1
LOOP

' DO-UNTIL
DO
    x = x + 1
LOOP UNTIL x >= 10
```

### Funktionen und Subroutinen

```basic
FUNCTION Add(a, b)
    Add = a + b
END FUNCTION

SUB PrintValue(v)
    message 1, v
END SUB

' Aufruf
result = Add(5, 3)
PrintValue(result)
```

### Eingebaute Funktionen

| Funktion | Beschreibung |
|----------|--------------|
| `SIN(x)` | Sinus |
| `COS(x)` | Cosinus |
| `TAN(x)` | Tangens |
| `ATAN(x)` | Arcus Tangens |
| `IIF(cond, true, false)` | Inline-IF |

### Konstanten

| Konstante | Wert |
|-----------|------|
| `PI` | 3.141592654 |
| `TRUE` | Boolean True |
| `FALSE` | Boolean False |
| `VBCRLF` | Carriage Return + Line Feed |
| `VBTAB` | Tab |
| `VBCR` | Carriage Return |
| `VBLF` | Line Feed |

### I/O-Befehle

```basic
' Debug-Ausgabe (geht zu ErrorCode)
DEBUGPRINT "Nachricht"
DEBUGCLEAR
DEBUGSHOW
DEBUGHIDE

' Message (Ergebnisausgabe mit Typ)
MESSAGE 1, "Erfolg"        ' Type 1
MESSAGE 2, "Warnung"       ' Type 2
MESSAGE 3, "Fehler"        ' Type 3

' UI-Dialoge (nur wenn AllowUi = true)
MSGBOX("Nachricht")
result = INPUTBOX("Frage", "Standard")
```

## Externe Variablen (Import)

```csharp
// C# Code - Variablen vorbereiten
dynamic imports = new ExpandoObject();
imports.clientName = "Mustermann GmbH";
imports.totalAmount = 1500.00;
imports.taxRate = 0.19;

engine.Imports = imports;
engine.PrepareMacro(id, script);
```

```basic
' Im Skript
IMPORT clientName
IMPORT totalAmount
IMPORT taxRate

DIM netAmount
netAmount = totalAmount / (1 + taxRate)

MESSAGE 1, clientName & ": Netto " & netAmount & " EUR"
```

## Fehlerbehandlung

### Fehlertypen

```csharp
public enum InputStreamErrors
{
    errGoBackPastStartOfSource = -2147221503,
    errInvalidChar = -2147221502,
    errGoBackNotImplemented = -2147221501
}

public enum LexErrors
{
    errUnknownSymbol = -2147221483,
    errUnexpectedEOF = -2147221482,
    errUnexpectedEOL = -2147221481
}

public enum ParsErrors
{
    errMissingClosingParent = -2147221473,
    errUnexpectedSymbol = -2147221472,
    errMissingLeftParent = -2147221471,
    errMissingComma = -2147221470,
    errNoYetImplemented = -2147221469,
    errSyntaxViolation = -2147221468,
    errIdentifierAlreadyExists = -2147221467,
    errWrongNumberOfParams = -2147221466,
    errCannotCallSubInExpression = -2147221465
}

public enum RunErrors
{
    errMath = -2147221443,
    errTimedOut = -2147221442,
    errCancelled = -2147221441,
    errNoUIallowed = -2147221440,
    errUninitializedVar = -2147221439,
    errUnknownVar = -2147221438
}
```

### Fehlerprüfung

```csharp
engine.PrepareMacro(id, script);

if (engine.ErrorNumber != 0)
{
    var error = code.ErrorObject;
    Console.WriteLine($"Fehler {error.Number}: {error.Description}");
    Console.WriteLine($"Zeile {error.Line}, Spalte {error.Col}");
    Console.WriteLine($"Quelltext: {error.ErrSource}");
}

var results = engine.Run();

if (engine.ErrorNumber != 0)
{
    Console.WriteLine($"Laufzeitfehler: {engine.ErrorCode}");
}
```

## Events

Die Code-Klasse bietet Events für I/O-Operationen:

```csharp
var code = new Code();

code.Message += (type, message) =>
{
    Console.WriteLine($"[Type {type}] {message}");
};

code.DebugPrint += (msg) =>
{
    Debug.WriteLine($"DEBUG: {msg}");
};

code.Assign += (name, value, ref accepted) =>
{
    // Externe Zuweisung behandeln
    if (externalVariables.ContainsKey(name))
    {
        externalVariables[name] = value;
        accepted = true;
    }
};
```

## Konfiguration

| Option | Beschreibung | Standard |
|--------|--------------|----------|
| `optionExplicit` | Variablen müssen mit DIM deklariert werden | `true` |
| `allowExternal` | IMPORT-Statement erlaubt | `true` |
| `AllowUi` | MsgBox/InputBox erlaubt | `false` |
| `CodeTimeout` | Timeout in Millisekunden | `120000` (2 Min) |

## Dateistruktur

```
Infrastructure/Scripting/
├── MacroEngine.cs                 # Haupt-Engine (IMacroEngine)
├── IMacroEngine.cs                # Interface
├── Code.cs                        # Bytecode & Executor
├── Opcodes.cs                     # Opcode-Enum
├── LexicalAnalyser.cs             # Tokenizer
├── StringInputStream.cs           # Eingabe-Stream
├── IInputStream.cs                # Stream-Interface
├── Symbol.cs                      # Token-Definitionen
├── SyntaxAnalyser.cs              # Parser (Hauptklasse)
├── SyntaxAnalyser.Expressions.cs  # Ausdruck-Parsing
├── SyntaxAnalyser.Statements.cs   # Statements & Deklarationen
├── SyntaxAnalyser.ControlFlow.cs  # IF, FOR, DO
├── SyntaxAnalyser.BuiltIns.cs     # Built-in Funktionen
├── Identifier.cs                  # Identifier-Typen
├── ScriptValue.cs                 # Boxing-freier Werttyp (NEU)
├── Scope.cs                       # Einzelner Scope
├── Scopes.cs                      # Scope-Stack
├── CompiledScript.cs              # Immutable kompiliertes Skript (NEU)
├── ScriptExecutionContext.cs      # Ausführungskontext (NEU)
├── ScriptResult.cs                # Ergebnis-Record (NEU)
├── ScriptError.cs                 # Fehler-Record (NEU)
├── ScriptTooComplexException.cs   # Rekursions-Exception (NEU)
├── ImportCache.cs                 # Reflection-Cache für Imports (NEU)
├── InterpreterError.cs            # Fehlerbehandlung
├── Helper.cs                      # Hilfs-Methoden
├── Formathelper.cs                # Format-Konvertierung
└── ScriptCode.cs                  # Script-Container
```

## Dependency Injection

```csharp
// In ServiceCollectionExtensions.cs
services.AddScoped<IMacroEngine, MacroEngine>();
```

```csharp
// In einem Service/Controller
public class MyService
{
    private readonly IMacroEngine _macroEngine;

    public MyService(IMacroEngine macroEngine)
    {
        _macroEngine = macroEngine;
    }

    public List<ResultMessage> ExecuteScript(string script, dynamic parameters)
    {
        _macroEngine.Imports = parameters;
        _macroEngine.PrepareMacro(Guid.NewGuid(), script);

        if (_macroEngine.ErrorNumber != 0)
        {
            throw new ScriptCompilationException(_macroEngine.ErrorCode);
        }

        return _macroEngine.Run();
    }
}
```

## Vergleich Frontend vs. Backend

| Aspekt | Frontend (TypeScript) | Backend (C#) |
|--------|----------------------|--------------|
| Sprache | TypeScript | C# |
| Parser-Struktur | Vererbungshierarchie | Partial Classes |
| Service | ScriptService (Angular Injectable) | MacroEngine (IMacroEngine) |
| I/O | Signals/Direct Return | Events |
| Timeout | 60 Sekunden | 120 Sekunden |
| UI-Support | Browser-Dialoge | Nicht implementiert |

## Beispiele

### Berechnung mit Import
```basic
IMPORT grundpreis
IMPORT rabatt
IMPORT mwst

DIM nettopreis, bruttopreis

nettopreis = grundpreis * (1 - rabatt / 100)
bruttopreis = nettopreis * (1 + mwst / 100)

MESSAGE 1, "Netto: " & nettopreis
MESSAGE 2, "Brutto: " & bruttopreis
```

### Bedingte Logik
```basic
IMPORT alter
IMPORT einkommen

DIM kategorie

IF alter < 18 THEN
    kategorie = "Minderjährig"
ELSE
    IF einkommen > 50000 THEN
        kategorie = "Premium"
    ELSE
        kategorie = "Standard"
    END IF
END IF

MESSAGE 1, kategorie
```

### Schleife mit Akkumulation
```basic
IMPORT anzahlMonate

DIM summe, i, zinssatz
zinssatz = 0.02
summe = 1000

FOR i = 1 TO anzahlMonate
    summe = summe * (1 + zinssatz)
NEXT

MESSAGE 1, "Endwert: " & summe
```

## Erweiterte Nutzung

### Thread-Safe Mehrfachausführung mit CompiledScript

```csharp
// Einmal kompilieren
var script = CompiledScript.Compile(@"
    IMPORT betrag
    IMPORT rabatt
    DIM ergebnis
    ergebnis = betrag * (1 - rabatt / 100)
    MESSAGE 1, ergebnis
");

if (script.HasError)
{
    throw new Exception(script.Error!.Description);
}

// Mehrfach ausführen (thread-safe)
async Task<ScriptResult> ExecuteAsync(double betrag, double rabatt, CancellationToken ct)
{
    script.SetExternalValue("betrag", betrag);
    script.SetExternalValue("rabatt", rabatt);

    var context = new ScriptExecutionContext(script);
    context.Message += (type, msg) => Console.WriteLine($"[{type}] {msg}");

    return context.Execute(ct);
}

// Parallel ausführen
var tasks = new[]
{
    ExecuteAsync(100, 10, CancellationToken.None),
    ExecuteAsync(200, 15, CancellationToken.None),
    ExecuteAsync(300, 20, CancellationToken.None)
};

var results = await Task.WhenAll(tasks);
```

### Mit CancellationToken abbrechen

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    var result = engine.Run(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Skript wurde abgebrochen");
}
```

### ScriptValue direkt verwenden

```csharp
// Werte erstellen
var number = ScriptValue.FromNumber(42.5);
var text = ScriptValue.FromString("Hallo");
var flag = ScriptValue.FromBoolean(true);

// Implizite Konvertierung
ScriptValue value = 100;        // FromInt
ScriptValue name = "Test";      // FromString
ScriptValue active = true;      // FromBoolean

// Werte auslesen
double d = value.AsDouble();    // 100.0
string s = name.AsString();     // "Test"
bool b = active.AsBoolean();    // true

// Arithmetik mit Operatoren
var sum = ScriptValue.FromNumber(10) + ScriptValue.FromNumber(5);  // 15
var neg = -ScriptValue.FromNumber(42);                              // -42

// Infinity-Handling
var inf = ScriptValue.FromNumber(1.0 / 0.0);
Console.WriteLine(inf.AsString());  // "∞"
```
