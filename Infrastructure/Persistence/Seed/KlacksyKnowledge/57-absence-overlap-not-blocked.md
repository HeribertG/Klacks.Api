---
name: explain_absence_overlap_not_blocked
description: |
  Explains whether Klacks stops a person from ending up with two overlapping absence entries. Wishes
  are never checked against each other or against booked absences. Booking several days at once
  (adopting a wish, dragging the fill handle, the assistant's booking) refuses a day that already
  has the same kind at overlapping times; booking a single cell has no check, and a different kind
  on the same day always goes through. Use this for double-booked or clashing absences.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - duplicate
  de:
    - überlappende absenz
    - doppelte absenz
  en:
    - overlapping absence
    - duplicate absence
synonyms:
  de: [kann ich zwei überlappende ferien anlegen, verhindert der server doppelte absenzen, wird eine überschneidende absenz abgelehnt, krank und ferien am gleichen tag, prüft klacks überlappende abwesenheiten, zwei absenzen am selben tag für dieselbe person]
  en: [can i create two overlapping vacations, does the server prevent duplicate absences, is an overlapping absence rejected, sick and vacation on the same day, does klacks check overlapping absences, two absences on the same day for the same person]
  fr: [puis-je créer deux congés qui se chevauchent, le serveur empêche-t-il les absences en double, une absence chevauchante est-elle refusée, maladie et congé le même jour]
  it: [posso creare due ferie sovrapposte, il server impedisce assenze doppie, un assenza sovrapposta viene rifiutata, malattia e ferie lo stesso giorno]
---

# Overlapping absences mostly go through unblocked

For most ways of entering an absence, Klacks does not check whether the same person already has an overlapping entry; the one safety net blocks only a repeat of the same kind, not every overlap.

<!-- level:short -->

## Clashing absences are mostly not stopped

Klacks trusts the planner when it comes to absences. You can enter two holiday requests for the same person that overlap, or a request on top of a day that is already booked as sick leave, and both are accepted without a warning. There is only a narrow safety net: when several days are booked in one go, Klacks refuses a day on which the person already has the same kind of absence. Example: booking "holiday" on a day that already is "holiday" is refused, while booking "sick leave" on a holiday day goes through. If you are unsure, ask Klacksy to check the period before you enter it.

<!-- level:elements -->

## Entering absences without creating clashes, step by step

1. **Ask first.** Ask Klacksy whether the person is free in the period; it runs the conflict check and lists overlapping wishes, overlapping booked absences, scheduled work and the membership window. The check only reports; it stops nothing.
2. **Enter a wish.** In the absence calendar (de: "Absenzen Kalender", en: "Absence Calendar") drag a kind onto the row or use (de: "+ Neuer Eintrag", en: "+ New entry"). No overlap is checked here.
3. **Book the wish.** In the schedule right-click the bar and choose (de: "Absenz übernehmen", en: "Adopt absence"). This books all days at once and is protected against a repeat of the same kind.
4. **Book single cells.** Typing an abbreviation into one cell or using the cell's right-click menu books one entry and is not protected.
5. **Clean up a double entry.** Delete a booked absence in the schedule, and a wish in the absence calendar with (de: "Eintrag löschen", en: "Delete entry").

<!-- level:effects -->

## Which route checks what

### Wishes: no check

Saving a wish needs a valid person, a valid kind and dates inside the person's membership period. Nothing compares it with other wishes or with booked absences of the same person.

### Booking several days at once: the same-kind guard

Several routes book through one common route that bundles the days of a request: adopting a wish (de: "Absenz übernehmen"), dragging the fill handle across days, and Klacksy's `add_break`. That route refuses a day when the person already has a booked absence of the same kind at overlapping times on that day, within the same plan (real plan against real plan, a scenario against the same scenario). The refusal is all-or-nothing: nothing is written, and when it happens while adopting a wish, the wish stays.

### Booking a single entry: no check

Typing an abbreviation into one cell, using the cell's right-click menu, dragging a booked absence to another person and changing an existing entry go through the single-entry route. It only checks that the day is not sealed, so two absences of the same kind on the same day are both saved.

### What the guard lets through

A different kind on the same day (sickness during an already booked holiday) is booked without objection, and so is an absence over a day that already has a shift.

### The advisory check

The feasibility check looks at overlapping wishes, overlapping booked absences, scheduled work and the membership window at once. It is advisory only: it reports what it found and does not stop anyone from saving anyway.

### Related skills

- `check_absence_conflicts` — the advisory pre-flight check, run before booking
- `add_break`, `add_break_placeholder` — the booking routes this topic describes

### Trigger phrases

- "Kann ich für dieselbe Person zwei sich überschneidende Ferienwünsche anlegen?"
- "Does the server block a duplicate sick day on top of a vacation?"
- "Verhindert Klacks doppelte Absenzen für dieselbe Person?"
- "Why was I able to book vacation and sick leave on the same day?"

<!-- level:pitfalls -->

## Consequences and edge cases

**Overlaps add up.** Every booked absence carries its own hours, and the hours of a period are summed over all entries. Two booked holiday entries on one day count twice.

**The guard compares times, not just the day.** Two whole-day entries overlap, so the second is refused. Entries recorded as a plain duration carry no times (start equals end) and are never seen as overlapping, so duplicates of those pass the guard.

**The route decides.** The same booking can be refused through (de: "Absenz übernehmen", en: "Adopt absence") and accepted when typed cell by cell. A refused adoption leaves the wish in place, so the next attempt sees the same conflict until the existing entry is deleted.

**A sealed day stops every booking route.** On a sealed day neither route books, whatever the overlap (de: "Tag ist gesperrt und kann nicht geändert werden", en: "Day is sealed and cannot be modified"). Wishes are not checked against seals.
