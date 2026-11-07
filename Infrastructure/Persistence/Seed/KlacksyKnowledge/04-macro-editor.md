---
name: explain_macro_editor
description: |
  Explains the Macro Editor in Klacks — a scripting tool that backs shifts
  and absences with formulas. Default macros are shipped for vacation,
  illness, accident and general shifts; custom macros compute exact hours
  (e.g. proportional to part-time percentage) and time credits in hours for
  night/weekend/holiday work, never money. Use this when the user asks how
  Klacks computes hours or time credits, how to override the default formula,
  or what "Macro" means.
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
  de: [makro, formel, skript, berechnung, zuschlag, zeitgutschrift, rechengrundlage, stundenformel]
  en: [macro, script, formula, calculation, surcharge, time credit, hours formula]
  fr: [macro, script, formule, calcul, supplément, crédit de temps]
  it: [macro, script, formula, calcolo, supplemento, credito di tempo]
---

# Macro Editor — Rechengrundlage für Stunden und Zeitgutschriften

## Kern-Idee (1 Satz)

Mit dem **Macro Editor** schreibt der Anwender Skripte, die Klacks als
Rechengrundlage verwendet — sie liefern die **exakte Dauer** (z.B. anteilig
zum Beschäftigungsgrad) und die **Zeitgutschriften** (Nacht-, Wochenend-,
Feiertags- und Überstunden-Zuschläge) pro Dienst oder Absenz. Zuschläge sind in
Klacks immer Zeitgutschriften in Stunden, nie Geld: Klacks plant und rechnet
keinen Lohn.

## Wofür Macros gut sind

| Bereich | Beispiel |
|---|---|
| **Ferien** | Std. = Vertragspensum × Tages-Soll, NICHT pauschal 8h. |
| **Krankheit / Unfall** | Stunden pro Ausfalltag anteilig zum Pensum. |
| **Militär / Zivildienst** | Kalenderspezifische Stunden-Berechnung. |
| **Allgemeine Dienste** | Standard-Berechnung der Arbeitszeit. |
| **Zuschläge** | Zeitgutschrift für Nacht, Sa, So, Feiertag, Überstunden — exakt nach Vertrag. |

## Default-Skripte

Klacks liefert ab Werk Macros für:

- Ferien
- Krankheit
- Unfall
- Allgemeine Dienste

Diese Default-Macros können **schnell angepasst** werden — das Skript wird im
Editor geöffnet, geändert, gespeichert. Beim nächsten Berechnungslauf nutzt das
System die neue Logik.

## Standard-Macro für neue Dienste und Bestellungen

Jeder **neu angelegte** Dienst bzw. jede neue Bestellung erhält automatisch das
Standard-Macro der Kategorie **Dienst** — egal ob in der Dienst-Maske, per
ERP-Import oder über Klacksy angelegt. Standard ist das Macro mit der Funktion
"Standard" (ab Werk **AllShift**); ist die Installation auf additive Zuschläge
eingestellt, das Macro mit der Funktion "Standard additiv" (ab Werk
**AllShiftAdditive**). Ein ausdrücklich gewähltes Macro wird nie überschrieben,
und gibt es kein aktives Standard-Macro der Kategorie Dienst, bleibt das Feld
leer — nur Klacksy legt in diesem Fall keinen Dienst an, sondern verlangt
zuerst ein Macro der Kategorie Dienst (Funktion Standard) oder ein ausdrücklich
gewähltes Macro. Bestehende Dienste werden nicht nachträglich geändert; ein entferntes
Macro bleibt entfernt.

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
- `explain_shift_lifecycle_order_to_shift` — Macros greifen erst ab der planbaren Schicht.
- `explain_surcharge_mode` — ohne Macro am Dienst entsteht keine Zeitgutschrift.

## Trigger-Phrasen

- "Was ist ein Macro?"
- "Wie berechnet Klacks Ferien-Stunden?"
- "Wie passe ich Nacht-Zuschläge an?"
- "How does the Klacks Macro Editor work?"
- "Comment Klacks calcule-t-il les heures de vacances ?"
- "Come funziona il calcolo del supplemento notturno?"
