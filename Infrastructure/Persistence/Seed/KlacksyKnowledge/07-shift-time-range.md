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

Ein Zeitfenster-Dienst definiert ein erlaubtes Zeitfenster und eine Einsatzdauer; wann genau innerhalb des Fensters gearbeitet wird, legst du erst beim Einplanen fest.

<!-- level:short -->

## Stufe 1 — Wofür ist das?

Ein Zeitfenster-Dienst ist ein Dienst ohne feste Uhrzeit: Du gibst nur an, in welchem Zeitfenster die Arbeit stattfinden darf und wie lange sie dauert. Beispiel: Ein Lieferdienst darf zwischen 08:00 und 20:00 Uhr fahren und dauert 4 Stunden. Ob die Fahrt dann von 09 bis 13 Uhr oder von 16 bis 20 Uhr stattfindet, entscheidest du erst beim Einplanen einer Person. So kannst du Aufträge planen, bei denen der Kunde nur ein Zeitfenster vorgibt und die genaue Uhrzeit offen ist.

<!-- level:elements -->

## Stufe 2 — So legst du ihn an und planst ihn ein

**Anlegen** (Dienste-Seite, Navi-Icon "Alle Dienste"):

1. In der Sicht "Bestellungen" (de: "Bestellungen", en: "Orders") auf **+ neue Bestellung** (de: "+ neue Bestellung", en: "+ New Order") klicken. Der Button erscheint nur mit dem Recht, Dienste anzulegen (Administratoren und Supervisoren).
2. In der Maske den Schalter **Experten Modus** (de: "Experten Modus", en: "Expert Mode") einschalten.
3. In der Karte **Stunden und Wochentage** (de: "Stunden und Wochentage", en: "Hours and Weekdays") **Von Zeit** und **Bis Zeit** als Fenstergrenzen sowie **Dauer** (de: "Dauer", en: "Duration") als Einsatzdauer eintragen und das Häkchen de: "Dienst innerhalb des Zeitrahmens (Von Zeit hh:mm - Bis Zeit hh:mm). Bitte in Feld Dauer die Dauer des Diensten angeben" (en: "Service within the time frame (From Time hh:mm - Until Time hh:mm). Please specify the duration of the service in the Duration field.") setzen. Bei Containern gibt es dieses Häkchen nicht.
4. Bestellung versiegeln (Schloss-Button in der Karte "Allgemeines"), damit der Dienst planbar wird.

**Erkennen:** In der Dienstliste trägt ein Zeitfenster-Dienst das Zeitfenster-Icon (ist er zusätzlich sporadisch, zeigt die Liste das Sporadik-Icon). In der Sicht "Planbare Dienste" blendet die Checkbox **Zeitbereich** (de: "Zeitbereich", en: "Time Range") diese Dienste ein oder aus.

**Einplanen** (Einsatzplan): Ziehst du den Dienst in eine Zelle, wählst ihn im Kontextmenü oder tippst sein Kürzel, öffnet sich der Dialog **Zeitfenster-Dienst einplanen** (de: "Zeitfenster-Dienst einplanen", en: "Plan time-window shift"):

- Der Dialog zeigt das Fenster und die Einsatzdauer des Dienstes und schlägt als Start den Fensteranfang mit der Dauer des Dienstes vor.
- Du änderst **Von** (de: "Von", en: "From") und/oder **Dauer** (de: "Dauer", en: "Duration"); **Bis** (de: "Bis", en: "To") wird immer aus Von plus Dauer berechnet und lässt sich nicht direkt ändern.
- Speichern ist nur möglich, wenn die Dauer größer als null ist und der gewählte Zeitraum ganz im Fenster liegt. Sonst erscheint die Meldung, dass die gewählte Zeit nicht vollständig im Zeitfenster liegt.
- **Abbrechen** (de: "Abbrechen", en: "Cancel") bucht nichts.

<!-- level:effects -->

## Stufe 3 — Zusammenspiel und Datenfluss

**Felder**

| Feld | Standard-Dienst | Zeitfenster-Dienst |
|---|---|---|
| `IsTimeRange` | `false` | `true` |
| `StartShift` | feste Start-Uhrzeit | **untere Fenstergrenze** |
| `EndShift` | feste End-Uhrzeit | **obere Fenstergrenze** |
| `WorkTime` | Dauer des Dienstes | **tatsächliche Einsatzdauer in Stunden** |

**Beispiel** `Lieferdienst` mit `StartShift=08:00`, `EndShift=20:00`, `WorkTime=4h`:

```
08  09  10  11  12  13  14  15  16  17  18  19  20
[================== Fenster (12h) ==================]
     [══════]                                            ← Variante 09–13
                  [══════]                               ← Variante 13–17
                                [══════]                 ← Variante 16–20
```

**Warum der Dialog:** Würde der Dienst mit seinen Fenstergrenzen gebucht, würde das ganze Fenster (hier 12 Stunden) als Arbeitszeit zählen. Darum bucht die Oberfläche immer einen gewählten Zeitraum innerhalb des Fensters, mit der gewählten Dauer als Arbeitszeit.

**Kombinierbar mit sporadisch:** Ein Dienst kann zugleich Zeitfenster- und sporadischer Dienst sein. Das Zeitfenster bestimmt, WANN am Tag gearbeitet werden darf; die Sporadik bestimmt, WIE OFT der Dienst in der Periode vorkommt (siehe `explain_shift_sporadic`).

**In Containern:** Ein Zeitfenster-Dienst im Bauplan eines Containers behält sein Fenster; die Lage im Fenster wird pro Bauplan-Eintrag festgelegt (Kontextmenü **Eigenschaften...**). Beim Buchen des Containers übernehmen die erzeugten Unter-Einsätze diese gewählte Zeit (siehe `explain_shift_container`).

**Dashboard:** Zeitfenster-Dienste zählen nicht im Dienste-Balken des Ressourcen-Monitors.

## Verwandte Skills

- `explain_shift_sporadic` — Kombi-Variante
- `explain_shift_container` — Zeitfenster-Dienste in Containern
- `explain_shift_lifecycle_order_to_shift` — Bestellung, Versiegeln und planbarer Dienst

## Trigger-Phrasen

- "Was ist ein Zeitfenster-Dienst?"
- "Wie funktioniert ein Zeitfenster-Dienst?"
- "Wie lege ich eine Schicht mit flexibler Startzeit an?"
- "How does TimeRange shift differ from a regular shift?"
- "Pourquoi mon employé peut-il choisir l'heure de début ?"

<!-- level:pitfalls -->

## Stufe 4 — Folgen, Grenzfälle, Endgültiges

- **Nach dem Versiegeln ist das Fenster fest.** Zeitfenster-Haken, Fenstergrenzen und Einsatzdauer gehören zu den gesperrten Feldern einer versiegelten Bestellung und ihrer planbaren Schicht. Ist das Fenster falsch, hilft nur Löschen und neu anlegen: Eine versiegelte Bestellung kann ein Administrator über Klacksy oder die API löschen, solange nichts darin verplant ist.
- **Die Prüfung "liegt im Fenster" sitzt im Dialog.** Er erzwingt Dauer größer null und Lage im Fenster, bevor gespeichert werden kann; die Fenstergrenzen gelten also beim Einplanen über diesen Dialog.
- **Das Fenster darf über Mitternacht laufen.** Gleiche Fenstergrenzen bedeuten ein ganzer Tag (24 Stunden). Beginnt das Fenster abends und wählst du einen Start nach Mitternacht, wird der Einsatz auf den Folgetag datiert, an dem er wirklich beginnt.
- **Einfügen mehrerer Zellen fragt nicht nach.** Beim Kopieren/Einfügen behält ein kopierter Einsatz seinen Zeitraum, wenn er ins Fenster passt; sonst wird die Einsatzdauer am Fensteranfang gebucht. Ein Dialog pro Zelle erscheint dabei nicht.
- **Der Dialog schlägt Fensteranfang plus Einsatzdauer vor.** Ist die Einsatzdauer des Dienstes länger als das Fenster, wird auf die Fensterlänge gekürzt; ist sie 0, gilt die ganze Fensterlänge. Die Dauer, die du im Dialog speicherst, wird die gebuchte Arbeitszeit.
- **Zeitfenster-Dienste fehlen im Kapazitätsbalken.** Wer sie anlegt, sieht sie im Dienste-Balken des Ressourcen-Monitors nicht; das ist gewollt, kein Datenfehler.
