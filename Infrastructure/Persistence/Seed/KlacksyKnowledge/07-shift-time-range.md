---
name: explain_shift_time_range
description: |
  Explains the TimeRange Shift in Klacks. A TimeRange shift has elastic working
  time inside a permitted window — StartShift / EndShift define the window
  boundaries, WorkTime defines the actual engagement duration. The dispatcher
  picks concrete StartTime/EndTime within the window when booking. Use this
  when the user asks how IsTimeRange differs from a fixed-hours shift, or how
  to schedule with a flexible start.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - timerange
  - zeitfenster
  - elastisch
  - flexibel
  - fenster
  - working window
  - flexible start
synonyms:
  de: [zeitfenster, elastische arbeitszeit, flexible schicht, einsatzdauer, fenster]
  en: [time range, working window, flexible shift, elastic, engagement duration]
  fr: [plage horaire, fenêtre de travail, horaire flexible, durée d'engagement]
  it: [fascia oraria, finestra di lavoro, turno flessibile, durata]
---

# TimeRange Shift — Fenster statt fixer Schichtzeit

## Kern-Idee (1 Satz)

Ein **TimeRange Shift** definiert ein **Zeitfenster** mit `StartShift` /
`EndShift` und eine **Einsatzdauer** `WorkTime` — die tatsächlichen
Start-/End-Zeiten einer Buchung legt der Disponent frei innerhalb dieses
Fensters fest.

## Felder

| Feld | Standard-Shift | TimeRange Shift |
|---|---|---|
| `IsTimeRange` | `false` | `true` |
| `StartShift` | feste Start-Uhrzeit | **untere Fenstergrenze** |
| `EndShift` | feste End-Uhrzeit | **obere Fenstergrenze** |
| `WorkTime` | implizit `EndShift - StartShift` | **tatsächliche Einsatz­dauer in Stunden** |

## Beispiel

`Lieferdienst` mit `StartShift=08:00`, `EndShift=20:00`, `WorkTime=4h`:

```
08  09  10  11  12  13  14  15  16  17  18  19  20
[================== Fenster (12h) ==================]
     [══════]                                            ← Variante 09–13
                  [══════]                               ← Variante 13–17
                                [══════]                 ← Variante 16–20
```

Der Disponent wählt beim Anlegen der Work konkrete `StartTime` / `EndTime`. Es
muss gelten:

- `StartTime ≥ shift.StartShift`
- `EndTime ≤ shift.EndShift`
- `EndTime − StartTime ≈ shift.WorkTime` (oder per Macro abgeleitet)

## Kombinierbar mit Sporadic

Ein Shift kann gleichzeitig `IsTimeRange` UND `IsSporadic` sein. Dann gilt:

- TimeRange bestimmt **WANN am Tag** gearbeitet werden darf.
- Sporadic bestimmt **WIE OFT** der Dienst in der Periode überhaupt stattfindet.

## Verwandte Skills

- `explain_shift_sporadic` — Kombi-Variante
- `explain_planning_assistant` — der Assistent platziert WorkTime im Fenster

## Trigger-Phrasen

- "Was ist ein Zeitfenster-Dienst?"
- "Wie funktioniert IsTimeRange?"
- "Wie lege ich eine Schicht mit flexibler Startzeit an?"
- "How does TimeRange shift differ from a regular shift?"
- "Pourquoi mon employé peut-il choisir l'heure de début ?"
