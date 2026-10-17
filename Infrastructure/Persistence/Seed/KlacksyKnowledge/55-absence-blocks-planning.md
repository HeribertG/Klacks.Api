---
name: explain_absence_blocks_planning
description: |
  Explains how a booked absence affects the automatic scheduling assistants. A booked absence is
  always untouchable for the wizards, unlike a work shift whose untouchability depends on its lock
  stage. Its hours count towards a person's target hours but never towards the weekly hours cap, and
  it interrupts a run of consecutive working days. Use this when asking whether the planner can move
  a booked holiday or why a sick week caused no overtime warning.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - harmonizer
  de:
    - wochenmaximum absenz
    - absenz unterbricht tage-serie
  en:
    - weekly hours cap absence
    - absence interrupts consecutive run
synonyms:
  de: [blockiert eine absenz die planung, verschiebt der assistent eine gebuchte absenz, zählt absenz zum wochenmaximum, unterbricht absenz die tage-serie, ist eine gebuchte absenz gesperrt für die planung, warum wurde meine krankheit nicht verschoben, darf der planer ferien verschieben]
  en: [does a booked absence block planning, can the wizard move a booked vacation, does absence count toward the weekly cap, does absence interrupt the consecutive run, is a booked absence locked for the planner, why was my sick day not moved]
  fr: [une absence bloque-t-elle la planification, l assistant peut-il déplacer une absence réservée, l absence compte-t-elle dans le maximum hebdomadaire, l absence interrompt-elle la série de jours]
  it: [un assenza prenotata blocca la pianificazione, l assistente può spostare un assenza prenotata, l assenza conta nel massimo settimanale, l assenza interrompe la serie di giorni]
---

# Booked absences and the planning assistants

For the automatic planning a booked absence is fixed from the moment it exists, whereas a work shift is only fixed once it has reached a lock stage.

<!-- level:short -->

## What the automatic planning does with absences

The automatic planning (the wizards that propose a schedule) never moves or deletes an absence that is booked. Holiday, sick leave and training stay exactly where they are, and the person is simply not planned on that day. A work shift is different: as long as nobody has confirmed it, the wizards may rearrange it. A mere wish for time off does not hold anybody back; only booked absences do. Example: if Anna has a booked holiday on Wednesday, the wizard plans the Wednesday shift for somebody else.

<!-- level:elements -->

## Making the planning respect an absence, step by step

1. **Book it.** A wish in the absence calendar is not enough. In the schedule right-click the wish bar and choose (de: "Absenz übernehmen", en: "Adopt absence", fr: "Reprendre l'absence", it: "Riprendere l'assenza"), or type the abbreviation of the kind into the person's cell. The bars of the wishes are shown with the toolbar switch (de: "Gewünschte Absenzen ein-/ausblenden", en: "Show/hide absence bars").
2. **Run the planning.** The wizard button in the schedule toolbar is visible for administrators only. Every run writes its result as a scenario, a draft next to the real plan.
3. **Check the draft.** The booked absences are unchanged in it, and the person has no shift on those days.
4. **Accept or reject.** In the scenario menu choose (de: "Szenario übernehmen", en: "Accept scenario") or (de: "Szenario verwerfen", en: "Reject scenario").
5. **To free the person for planning again,** delete the booked absence in the schedule; deleting only the wish changes nothing.

<!-- level:effects -->

## What exactly the assistants read, and what counts

### Which absences the wizards see

The automatic planning first builds a plan (called the first wizard below) and then improves it with the harmonizers. They read the booked absences of the plan they work on: the real plan, or the scenario they are filling. A scenario works on its own copy of the absences. When the scenario is accepted, that copy replaces the real absences of its scope; absences that are confirmed or higher, or that lie on a locked day, stay as they are. Wishes are never read.

### The whole day is blocked

A booked absence blocks the person for the whole calendar day, whatever times the absence has: a two-hour training in the morning still keeps the person out of every shift that day. An absence that belongs to a container service does not count as an absence for the first wizard.

### Hours

Hours of a booked absence are added to the worked hours of the period, so somebody on holiday moves towards the guaranteed hours as if they had worked. The first wizard measures the period maximum against the hours already booked in the pay period before the planned range, absences included. The weekly maximum is checked only by the harmonizer; there an absence day is not a working day, so absence hours do not count towards that weekly cap.

### Consecutive working days

An absence day is not a working day, so it interrupts a run. Three working days, a day of holiday, then three more working days is not a run of six, and a rule capping runs at five days is not violated.

### Availability and planning commands

On a day with a booked absence or a planning command, the person's availability blocks are dropped, so the higher layer rules the day. Qualification limits stay.

### Related skills

- `add_break`, `update_break`, `delete_break` — placing the booked absence itself
- `cover_absence` — find replacement cover once an absence is booked

### Trigger phrases

- "Verschiebt der Planungs-Assistent eine gebuchte Absenz?"
- "Does a sick week count against the weekly hours limit?"
- "Zählt eine Ferienwoche gegen die Sechs-Tage-Regel?"
- "Why didn't the wizard reschedule this vacation?"

<!-- level:pitfalls -->

## Consequences and edge cases

**A work shift becomes fixed only through its lock, but a booked absence needs none.** The lock stage of an absence is irrelevant for the wizards: they load every booked absence, confirmed or not. Work entries are fixed from the stage confirmed upwards, and also when they lie before the start date of a re-planning run, or when they carry a replacement or a correction.

**Wishes protect nobody.** A person with only a wish can be planned on that day. Book the wish before you run the planning.

**The replacement search treats absences differently from the wizards.** For finding a replacement, a candidate with any booked absence that day is excluded, except for a kind marked as on call: such a candidate is not treated as absent and is offered first. The wizards still treat an on-call day as occupied.

**Time of day is ignored.** An absence entered with a time range does not leave the rest of the day free for the wizards.

**Where the weekly maximum is checked.** Only the harmonizer checks a weekly maximum, and there absence hours are excluded. The first wizard knows the period maximum instead, and that one includes the absence hours booked before the planned range.
