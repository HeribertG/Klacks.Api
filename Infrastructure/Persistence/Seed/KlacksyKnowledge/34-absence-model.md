---
name: explain_absence_model
description: |
  Explains the three different things called an absence: the absence type as master data, the actual
  absence booked on a person and a date, and a pre-booked wish that reserves nothing yet. Covers how
  a wish becomes a real absence, that absence hours count towards target hours, and which counting
  rules an absence type carries for weekends and public holidays. Use this when the user asks why a
  requested holiday does not show in the schedule, how to create a new kind of absence, or how
  absences affect the hours balance.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - absenztyp
  - absence type
  - ferienwunsch
  - vorgemerkt
  - urlaubsantrag
  - zählt absenz
synonyms:
  de: [absenztyp, absenzart, ferienwunsch, urlaubsantrag, vorgemerkt, vorgeplante absenz, wird nicht angezeigt, zählt die absenz zu den stunden, neue absenzart anlegen]
  en: [absence type, holiday request, pre-booked, wish, does not show in the schedule, do absences count towards hours, create a new absence kind]
  fr: [type d absence, demande de congé, pré-réservé, n apparaît pas dans le planning, les absences comptent-elles]
  it: [tipo di assenza, richiesta di ferie, pre-prenotato, non appare nel piano, le assenze contano nelle ore]
---

# Absences — three different things with similar names

"Absence" can mean a kind of absence, a wish for time off, or an absence that is really booked — and mixing them up is the most common reason a booked holiday seems to have vanished.

<!-- level:short -->

## What an absence is

In Klacks an absence is time when somebody is away: holiday, sick leave, training. It exists in two forms that look alike but behave differently. A **wish** is only a note in the absence calendar — it shows that somebody would like to be off, but it reserves nothing. A **booked absence** is a real entry in the schedule: from then on the person counts as away. A planner turns a wish into booked absences once the time off is approved. Example: Anna asks for the first week of July off; until a planner books it, her row in the schedule shows nothing for that week — nothing is broken, the request is simply still a wish.

<!-- level:elements -->

## Working with absences, step by step

1. **Enter a wish.** Open the absence calendar (de: "Absenzen Kalender", en: "Absence Calendar", fr: "Calendrier d'absences", it: "Calendario assenze"). Either drag a coloured kind from the legend onto the person's row — that creates an entry with the kind's default length — or select the row and click (de: "+ Neuer Eintrag", en: "+ New entry", fr: "+ Nouvelle entrée", it: "+ Nuova voce") and fill in the dates, the type and the notes in the edit mask below.
2. **See wishes in the schedule.** In the schedule toolbar switch on (de: "Gewünschte Absenzen ein-/ausblenden", en: "Show/hide absence bars", fr: "Afficher/masquer les barres d'absence", it: "Mostra/nascondi barre assenza"). The wishes appear as bars over the person's days. They are a reminder only.
3. **Book a wish.** Right-click the bar and choose (de: "Absenz übernehmen", en: "Adopt absence", fr: "Reprendre l'absence", it: "Riprendere l'assenza"). If the kind has extended absences (de: "Erweiterte Absenzen", en: "Extended Absences"), you pick one of them in a submenu. Every calendar day of the wish becomes a booked absence and the wish disappears.
4. **Book a single day.** Type the abbreviation of the kind into the person's cell, or pick the kind in the right-click menu of the cell.
5. **Recognise what is booked.** In the absence calendar a booked entry is marked (de: "Gebucht", en: "Booked", fr: "Réservé", it: "Prenotato") in the notes column of the list. Booked entries are read-only there; change or delete them in the schedule.
6. **Create a new kind of absence.** Open the settings, section (de: "Abwesenheit & Kalender", en: "Absence & Calendar"), card (de: "Absenzen", en: "Absences"), button (de: "+ Absenz hinzufügen", en: "+ Add Absence"). Fill in name, abbreviation and colour, the default length (de: "Standardmässig Anzahl Tage", en: "Default Number of Days") and the value per day (de: "Berechneter Wert pro Tag", en: "Calculated Value per Day").

<!-- level:effects -->

## How the three things connect

### The kind of absence

A kind is master data created once in the settings: a name and an abbreviation in several languages, a colour, a default length in days, a value per day and optionally a calculation macro. Its switches for Saturday, Sunday and holidays are recorded on the kind but no calculation reads them. A change to a kind affects future bookings; existing bookings keep their stored hours.

### The wish

A wish is stored per person with a first and a last day and a note. Saving it needs only a valid person, a valid kind and dates inside the person's membership period. It reserves nothing: the schedule does not block a cell because of it, and the automatic planning does not read wishes at all. It shows in the absence calendar, as bars in the schedule when the toolbar switch is on, and in the grey absence bars of the dashboard's capacity view.

### The booked absence

A booked absence is one entry per person and day with start and end time and hours. It appears in the schedule and, marked as booked, in the absence calendar. The automatic planning treats it as fixed. Its hours are added to the worked hours of the period, which the schedule's row header shows next to the guaranteed hours (tooltips: de: "Geleistete Stunden", en: "Worked Hours", and de: "Garantierte Stunden", en: "Guaranteed Hours") — so somebody on holiday does not fall behind their contract. How many hours an entry carries is decided when it is booked, by the kind's macro, not by the VALUE column of the calendar list. Booking, changing and deleting a booked absence is refused on a sealed day (de: "Tag ist gesperrt und kann nicht geändert werden", en: "Day is sealed and cannot be modified").

### What Klacksy can check

Beyond booking and deleting, absences can be examined: conflicts for one person, a summary per person, and the overlap within a group — the "who else is away that week" question that decides whether a request can be granted.

### Related skills

- `list_absence_types` / `create_absence_type` / `update_absence_type` / `delete_absence_type` — the kinds
- `add_break` / `update_break` / `delete_break` — the booked absence on a person and date
- `add_break_placeholder` / `update_break_placeholder` / `delete_break_placeholder` — wishes
- `check_absence_conflicts` / `get_client_absence_summary` / `get_group_absence_overlap` — checks
- `cover_absence` — find cover for an absent person

### Trigger phrases

- "The holiday request is not showing in the schedule."
- "Create a new absence kind for further training."
- "Do holidays count towards the hours owed?"
- "Who else is away that week?"
- "Was ist der Unterschied zwischen Wunsch und Absenz?"

<!-- level:pitfalls -->

## Consequences and edge cases

**A wish blocks nothing, so double planning is possible.** The schedule shows no warning when a shift is placed on a day with a wish, and the automatic planning does not know the wish and may plan the person anyway. Book the wish first when the time off is certain.

**Booking a wish is a one-way step.** It creates one booked absence for every calendar day of the wish, weekends and public holidays included, and then deletes the wish. The wish does not come back; to reverse it, delete the booked days in the schedule and enter a new wish. If the booking is refused (for example because the person already has the same kind of absence at the same time), nothing is written and the wish stays.

**Typing an abbreviation only works for kinds without extended absences.** A kind that has extended absences is booked through the right-click menu, because the extended absence has to be chosen.

**Sealed days.** Booked absences cannot be created, changed or deleted on a sealed day; an administrator has to reopen the period first. Wishes are not checked against seals.

**Deleting a kind.** The deletion is refused while any booking or wish still uses the kind. Klacksy additionally refuses the protected built-in kinds; to retire a kind there, hide it in the absence calendar instead (de: "Nur für den internen Gebrauch", en: "For internal use only").

**Duplicate names.** Klacksy refuses to create a second kind with the same name; the settings page does not check, so two kinds with the same name are possible there.

**The counting switches do nothing today.** (de: "Samstag wird berechnet", en: "Saturday is calculated"), the Sunday switch and (de: "Feiertage werden berechnet", en: "Holidays are calculated") are stored but evaluated nowhere; toggling them changes no hours.

**Naming trap for Klacksy.** The skill that creates a new *kind* is not the one that books an absence for somebody. Booking works through `add_break` referencing an existing kind; without a given hours value it books eight hours per day. Recording a wish is `add_break_placeholder`. Getting this the wrong way round creates master data nobody wanted.
