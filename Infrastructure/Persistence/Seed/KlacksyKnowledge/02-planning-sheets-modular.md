---
name: explain_planning_sheets_modular
description: |
  Explains the Planungsblatt (planning sheet) concept in Klacks — a container
  that holds references to individual employee rows. Use this when the user
  wonders why changes on employee Y in sheet 1 also appear in sheet 2, or how
  Klacks structures planning data across multiple groups/business units.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - planungsblatt
  - planning sheet
  - mitarbeiterzeile
  - employee row
  - container
  - referenz
  - shared employee
synonyms:
  de: [planungsblatt, planungsbogen, einsatzblatt, mitarbeiterzeile, referenz, geteilte sicht]
  en: [planning sheet, plan sheet, employee row, shared reference, container]
  fr: [feuille de planification, ligne d'employé, conteneur, référence]
  it: [foglio di pianificazione, riga del dipendente, contenitore, riferimento]
---

# Planungsblatt — der Container für Mitarbeiterzeilen

## Kern-Idee (1 Satz)

Ein **Planungsblatt** ist ein Container mit Verweisen auf einzelne
Mitarbeiterzeilen — derselbe Mitarbeiter kann gleichzeitig in mehreren Blättern
vorkommen, jede Änderung wird automatisch in allen Blättern sichtbar.

## Aufbau

```
Planungsblatt 1                  Planungsblatt 2
─────────────────                ─────────────────
[Mitarbeiter Y] ───── REF ─────► [Mitarbeiter Y]   ← dieselbe Person
[Mitarbeiter X]                  [Mitarbeiter Z]
[Mitarbeiter W]                  [Mitarbeiter Y']  ← falls Y mehrfach geteilt
```

Die Mitarbeiterzeile ist die kleinste planbare Einheit. Das Planungsblatt
ist nur eine Sammlung von Verweisen darauf — kein Daten-Duplikat.

## Konsequenz für die Bedienung

- Änderung am Mitarbeiter in **einem** Blatt → sichtbar in **allen** Blättern.
- Konflikte (Doppelbelegung in zwei Geschäftsfeldern) werden technisch verhindert,
  weil die Mitarbeiterzeile nur einen Plan-Wert pro Tag hat.
- Geschäftsfelder können sich Mitarbeiter ausleihen, ohne dass ein Planer das
  manuell synchronisieren muss.

## Verwandte Skills

- `explain_planning_divide_et_impera` — warum modular geplant wird
- `explain_planning_assistant` — wer das Blatt füllen hilft

## Trigger-Phrasen

- "Was ist ein Planungsblatt?"
- "Warum sehe ich dieselbe Änderung in mehreren Plänen?"
- "How do shared employees work across planning sheets in Klacks?"
- "Wie verhindert Klacks Doppelbelegungen zwischen Abteilungen?"
