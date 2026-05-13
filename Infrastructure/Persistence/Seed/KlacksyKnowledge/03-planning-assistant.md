---
name: explain_planning_assistant
description: |
  Explains the Planungs-Assistent (planning assistant / autofill) in Klacks. It
  computes proposed shift assignments based on the shifts in the plan and each
  employee's individual settings, presenting the result as a SCENARIO that the
  planner must explicitly accept. Use this when the user asks how autofill
  works, why the assistant only suggests instead of deciding, or how scenarios
  differ from the real plan.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - assistant
  - planungsassistent
  - autofill
  - vorschlag
  - scenario
  - szenario
  - wizard
synonyms:
  de: [assistent, autofill, vorschlag, planungshilfe, szenario, planungsvorschlag, wizard]
  en: [planning assistant, autofill, suggestion, scenario, wizard, proposal]
  fr: [assistant de planification, suggestion, scénario, proposition]
  it: [assistente di pianificazione, suggerimento, scenario, proposta]
---

# Planungs-Assistent — Vorschlagsmaschine, nicht Entscheider

## Kern-Idee (1 Satz)

Der **Planungs-Assistent** berechnet auf Basis der dem Planungsblatt zugewiesenen
Dienste und der individuellen Einstellungen jedes Mitarbeiters einen
**Vorschlag** und zeigt ihn als **Szenario** an — die finale Annahme oder
Ablehnung liegt beim Planer.

## Grundregel

> **Der Planungs-Assistent macht nur Vorschläge, der Planer macht die Planung.**

## Workflow

1. Planer klickt die Assistenten-Schaltfläche im Planungsblatt.
2. Assistent liest:
   - alle dem Blatt zugewiesenen Dienste (Tasks, Container, Sporadic, TimeRange)
   - die Mitarbeiter-Einstellungen (Verträge, Verfügbarkeit, Diensttypwünsche)
3. Berechnet einen Vorschlag.
4. Stellt das Ergebnis als **Szenario** in der Schedule-Ansicht dar
   (`AnalyseToken` markiert Szenario-Zeilen — sie überlagern den realen Plan
   ohne ihn zu verändern).
5. Planer prüft, kann das Szenario **annehmen** (Promote → reale Works) oder
   **verwerfen** (Discard → Szenario-Klone werden soft-deleted).

## Was der Assistent NICHT macht

- Er entscheidet **nicht** automatisch.
- Er überschreibt **nicht** existierende, gesealte oder gruppen-gesperrte Zellen.
- Er ignoriert **keine** Diensttypwünsche, sondern versucht sie zu erfüllen
  (siehe `explain_shift_type_preferences`).

## Verwandte Skills

- `explain_planning_sheets_modular` — der Container, den der Assistent füllt
- `explain_shift_type_preferences` — wie Wünsche den Vorschlag steuern
- `explain_macro_editor` — wie Stunden und Zuschläge ausgerechnet werden

## Trigger-Phrasen

- "Wie funktioniert der Planungs-Assistent?"
- "Was ist ein Szenario in Klacks?"
- "Warum übernimmt der Assistent meine Pläne nicht direkt?"
- "How does the Klacks autofill assistant work?"
- "Comment fonctionne l'assistant de planification ?"
