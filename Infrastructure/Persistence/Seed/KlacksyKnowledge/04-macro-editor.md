---
name: explain_macro_editor
description: |
  Explains the Macro Editor in Klacks — a scripting tool that backs shifts,
  employments and wage components with formulas. Default macros are shipped
  for vacation, illness, accident and general shifts; custom macros compute
  exact hours (e.g. proportional to part-time percentage) and surcharges for
  night/weekend/holiday work. Use this when the user asks how Klacks computes
  hours/costs, how to override the default formula, or what "Macro" means.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - macro
  - makro
  - script
  - skript
  - berechnung
  - zuschlag
  - ferien
  - formel
synonyms:
  de: [makro, formel, skript, berechnung, zuschlag, lohn, rechengrundlage, lohnformel]
  en: [macro, script, formula, calculation, surcharge, payroll, allowance]
  fr: [macro, script, formule, calcul, supplément, salaire]
  it: [macro, script, formula, calcolo, supplemento, stipendio]
---

# Macro Editor — Rechengrundlage für Dienste, Beschäftigung und Lohn

## Kern-Idee (1 Satz)

Mit dem **Macro Editor** schreibt der Anwender Skripte, die Klacks als
Rechengrundlage verwendet — sie liefern die **exakte Dauer** (z.B. anteilig
zum Beschäftigungsgrad) oder die **exakten Kosten** (z.B. Nacht-, Wochenend-
und Feiertags-Zuschläge) pro Dienst, Beschäftigung oder Lohnkomponente.

## Wofür Macros gut sind

| Bereich | Beispiel |
|---|---|
| **Ferien** | Std. = Vertragspensum × Tages-Soll, NICHT pauschal 8h. |
| **Krankheit / Unfall** | Pensum-anteilige Lohnfortzahlung. |
| **Militär / Zivildienst** | Kalenderspezifische Stunden-Berechnung. |
| **Allgemeine Dienste** | Standard-Berechnung der Arbeitszeit. |
| **Zuschläge** | Nacht, Sa, So, Feiertag, Überstunden — exakt nach Vertrag. |

## Default-Skripte

Klacks liefert ab Werk Macros für:

- Ferien
- Krankheit
- Unfall
- Allgemeine Dienste

Diese Default-Macros können **schnell angepasst** werden — das Skript wird im
Editor geöffnet, geändert, gespeichert. Beim nächsten Berechnungslauf nutzt das
System die neue Logik.

## Anbindung im Datenmodell

Ein `Shift` trägt eine optionale `MacroId` (FK auf Macro-Tabelle). Beim
Speichern eines `Work` läuft `WorkMacroService.ProcessWorkMacroAsync(work)`:

1. Shift laden, `MacroId` prüfen.
2. `IMacroDataProvider.GetMacroDataAsync(work)` baut den Input.
3. `IMacroCompilationService.CompileAndExecuteAsync(macroId, data)` führt das
   Skript aus.
4. Resultat schreibt sich in `work.Surcharges` (oder analoges Feld).

## Verwandte Skills

- `explain_planning_assistant` — der Assistent nutzt Macro-Resultate als Soll.
- `explain_shift_lifecycle` — Macros greifen erst ab `OriginalShift`.

## Trigger-Phrasen

- "Was ist ein Macro?"
- "Wie berechnet Klacks Ferien-Stunden?"
- "Wie passe ich Nacht-Zuschläge an?"
- "How does the Klacks Macro Editor work?"
- "Comment Klacks calcule-t-il les heures de vacances ?"
- "Come funziona il calcolo del supplemento notturno?"
