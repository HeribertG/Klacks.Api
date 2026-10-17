---
name: explain_shift_holiday_options
description: |
  Explains whether a shift runs on a public holiday. Without a holiday option a shift skips every holiday
  that falls on one of its ticked days. "Plus every public holiday" (FT) adds every holiday, whatever the
  weekday. "Selected days, holidays included" (+FT) keeps exactly the ticked days, even when one is a
  holiday, while holidays on unticked days do not count. Use when someone asks whether a shift runs at
  Easter or Christmas, what FT or +FT means, or why a shift is missing on a holiday.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - +ft
    - +hol
  de:
    - dienst an feiertagen
    - feiertagsoption
    - was bedeutet ft
  en:
    - shift on public holidays
    - holiday option
    - what does hol mean
synonyms:
  de: [läuft mein dienst an feiertagen, was bedeutet ft, was bedeutet +ft, dienst an ostern, dienst an weihnachten, warum fehlt mein dienst am feiertag, unterschied ft und +ft]
  en: [does my shift run on public holidays, what does hol mean, what does +hol mean, shift on easter, shift on christmas day, why is my shift missing on the holiday, difference between hol and +hol]
  fr: [mon service a-t-il lieu les jours fériés, que signifie jf, service à pâques, service le jour de noël, pourquoi mon service manque le jour férié]
  it: [il mio turno si svolge nei giorni festivi, cosa significa fer, turno a pasqua, turno a natale, perché manca il mio turno nel giorno festivo]
---

# Dienst an Feiertagen — die beiden Feiertagsoptionen

Die Wochentags-Häkchen legen fest, an welchen Tagen ein Dienst läuft; die zwei Feiertagsoptionen legen fest, was an einem Feiertag passiert.

<!-- level:short -->

## Stufe 1 — Was bedeuten die Optionen?

Ein Dienst läuft an den Wochentagen, die du ankreuzt. Fällt auf einen dieser Tage ein Feiertag, fällt der Dienst normalerweise aus. Mit zwei Zusatzoptionen änderst du das:

- **Zusätzlich an jedem Feiertag** (de: "Zusätzlich an jedem Feiertag", en: "Plus every public holiday", fr: "En plus chaque jour férié", it: "In più ogni giorno festivo"), in der Dienstliste als **FT** (de: "FT", en: "HOL", fr: "JF", it: "FER") gezeigt: Der Dienst läuft an den gewählten Tagen und zusätzlich an jedem Feiertag, egal auf welchen Wochentag er fällt.
- **Gewählte Tage, auch an Feiertagen** (de: "Gewählte Tage, auch an Feiertagen", en: "Selected days, holidays included", fr: "Jours choisis, fériés inclus", it: "Giorni scelti, festivi inclusi"), in der Dienstliste als **+FT** (de: "+FT", en: "+HOL", fr: "+JF", it: "+FER") gezeigt: Der Dienst läuft genau an den gewählten Tagen, auch wenn einer davon ein Feiertag ist. Feiertage an nicht gewählten Tagen zählen nicht.

Beispiel mit Montag bis Mittwoch gewählt:

| Mo–Mi gewählt | Feiertag am Mo | Feiertag am Fr |
|---|---|---|
| ohne Zusatz | kein Dienst | kein Dienst |
| Zusätzlich an jedem Feiertag (FT) | Dienst | Dienst |
| Gewählte Tage, auch an Feiertagen (+FT) | Dienst | kein Dienst |

<!-- level:elements -->

## Stufe 2 — Wo stelle ich das ein, und was wähle ich?

Die Häkchen stehen in der Maske des Dienstes in der Karte **Stunden und Wochentage** (de: "Stunden und Wochentage", en: "Hours and Weekdays", fr: "Heures et jours de la semaine", it: "Ore e Giorni Feriali"), direkt unter den Wochentagen Montag bis Sonntag. Du kannst nur eine der beiden Optionen setzen; setzt du die eine, verschwindet das Häkchen bei der anderen.

Typische Fälle:

| Fall | Einstellung |
|---|---|
| Büro Montag bis Freitag, an Feiertagen geschlossen | Mo–Fr ankreuzen, keine Feiertagsoption |
| Spital oder Pflege, jeden Tag besetzt, auch an Feiertagen | alle sieben Tage ankreuzen und "Gewählte Tage, auch an Feiertagen" |
| Zusatzdienst nur an Feiertagen, zum Beispiel ein Notfalldienst | keinen Wochentag ankreuzen, nur "Zusätzlich an jedem Feiertag" |
| Wochenenddienst, der zusätzlich an jedem Feiertag läuft | Sa und So ankreuzen und "Zusätzlich an jedem Feiertag" |

<!-- level:effects -->

## Stufe 3 — Was bewirkt die Einstellung?

- **Einsatzplan:** Der Dienst wird nur an den passenden Tagen zum Einplanen angeboten. An einem Feiertag ohne passende Option erscheint er dort nicht.
- **Bestehende Einträge bleiben:** Wer schon eingeplant ist, bleibt eingeplant. Die Option entscheidet nur, an welchen Tagen der Dienst neu angeboten wird.
- **Nach dem Versiegeln gesperrt:** Wie die Wochentage lässt sich die Feiertagsoption nach dem Versiegeln der Bestellung in der Maske nicht mehr ändern. Lege sie deshalb vor dem Versiegeln richtig fest.
- **Container:** Ein Container hat für Feiertage eine eigene Feiertagsvorlage, also einen eigenen Tagesplan. Bei "Zusätzlich an jedem Feiertag" gilt sie an jedem Feiertag, bei "Gewählte Tage, auch an Feiertagen" nur an einem Feiertag, der auf einen gewählten Wochentag fällt.

<!-- level:pitfalls -->

## Stufe 4 — Worauf achten?

- **Mit allen sieben Tagen gleichwertig:** Sind bei einem Dienst alle sieben Wochentage angekreuzt, führen beide Optionen zum selben Ergebnis: Er läuft jeden Tag.
- **"Gewählte Tage, auch an Feiertagen" braucht mindestens einen gewählten Tag:** Ohne angekreuzten Wochentag läuft der Dienst damit an keinem Tag, und die Maske markiert die Wochentage als fehlerhaft. Für einen reinen Feiertagsdienst ist "Zusätzlich an jedem Feiertag" die richtige Option.
- **Welche Tage Feiertage sind,** bestimmt der Feiertagskalender, der im Einsatzplan verwendet wird. Fehlt dort ein Feiertag, greift auch keine Feiertagsoption, und der Tag zählt als normaler Wochentag.

## Siehe auch

- `explain_page_shifts` — die Dienste-Seite und die Maske im Überblick
- `explain_shift_container` — Container mit Tagesvorlagen
