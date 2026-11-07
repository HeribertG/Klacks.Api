---
name: explain_absence_cover_move
---

# Covering an absence over a planned shift

When a person who has a planned shift becomes absent, the shift either moves completely to the person who covers it, or it is deleted when nobody covers it and it has not started.

<!-- level:short -->

## What happens to the shift of an absent person

An absence on a day with a planned shift does not leave two contradicting entries behind. If someone takes over the whole shift, the shift moves to that person with everything attached to it. If nobody covers it and it has not started yet, it is deleted, and the open slot shows the gap so the planner keeps looking. Example: Elsa reports sick for tomorrow's early shift; Tom takes it, so the shift with its travel time and expenses now belongs to Tom, and the plan marks it as taken over from Elsa.

<!-- level:elements -->

## Covering an absence, step by step

1. **In the schedule:** right-click the shift of the absent person and choose (de: "Ausfall decken...", en: "Cover absence...", fr: "Couvrir l'absence...", it: "Copri assenza..."). The dialog (de: "Ausfall decken", en: "Cover absence (Recovery)", fr: "Couvrir une absence (récupération)", it: "Copri assenza (recupero)") books the absence and proposes people who can take the shift. Its result is a scenario.
2. **Accept the scenario.** Only then does the move become real: a fully covered shift moves to the replacement, an uncovered shift that has not started is deleted.
3. **Absence entered directly:** booking an absence over a day with shifts first shows (de: "Dienste werden gelöscht", en: "Services will be deleted", fr: "Des services seront supprimés", it: "I turni verranno eliminati") with the shifts concerned. Confirming with (de: "Absenz eintragen", en: "Enter absence", fr: "Saisir l'absence", it: "Inserisci assenza") books the absence and deletes those shifts at once.
4. **Check the error list.** A shift that stays in place next to an absence is reported as (de: "Absenz nicht gedeckt: {{absence}}, Dienst {{shifts}} bleibt ohne Ablösung bestehen", en: "Absence not covered: {{absence}}, shift {{shifts}} still stands without a replacement"). Delete or shorten it by hand, or cover it.

<!-- level:effects -->

## What moves, what is deleted, what stays

### Full cover: the shift moves

A shift moves when one person covers exactly the whole shift and nothing holds it in place: no lock stage, no replacement, not a task inside a container. It moves with briefing, debriefing, travel time, corrections, expenses and, for a container, all tasks inside it. The time credits (night, weekend, holiday) are recalculated with the contract of the new person. The schedule shows an icon with the tooltip (de: "Übernommen von {{name}}", en: "Taken over from {{name}}", fr: "Repris de {{name}}", it: "Ripreso da {{name}}"), or (de: "Aus einem Ausfall übernommen", en: "Taken over after an absence") when the original person is not visible to you. A moved shift is fixed for all planning wizards; they never move it again.

### No cover: the shift is deleted

A shift that nobody covers and that has not started does not take place: it is deleted together with the tasks of a container, its expenses disappear with it, and it is not charged to the customer. The hours of the absence are calculated before the deletion. This happens when the scenario is accepted (for every scenario with a group) or right away when the absence is entered directly. An on-call absence (an absence type marked as on-call duty) never takes a shift away; the shift stays and is reported as an on-call overlap warning.

### Never deleted

- shifts that have already started
- shifts with a lock stage (confirmed, approved or closed), and containers with a locked task inside
- shifts that already have a replacement, and tasks inside a container
- shifts of another group
- everything when no group is selected
- any shift under an on-call absence

Klacksy's chat sends no selected group, so an absence booked through Klacksy never deletes a shift; the shift stays and the error list reports the uncovered absence. Only when Klacksy prepares a cover for a named group does its scenario delete uncovered shifts once accepted.

### Partial absences

An absence with a duration but no times counts as the whole day. An absence with times takes away a shift only if the shift lies completely inside it; otherwise the shift stays and appears in the error list. A night shift running past midnight is never taken away by a partial absence.

### Related skills

- `cover_absence` — prepare a cover as a scenario for a group
- `find_replacement` — who could take a shift
- `get_replacement_status` — what came of the requests
- `add_break` — book an absence (never deletes shifts, see above)

### Trigger phrases

- "Elsa ist krank, was passiert mit ihrem Dienst?"
- "Why was the shift deleted when I entered the absence?"
- "Wieso steht beim Dienst übernommen von?"
- "Who gets the expenses when somebody covers a shift?"

<!-- level:pitfalls -->

## Consequences and edge cases

**The route decides between moving and replacing.** The planner's dialog always treats the person as not having started, so a full cover moves the shift. Through Klacksy or a messenger, the shift moves only when the absence was reported before the shift starts; reported later, the cover becomes a replacement (the original person keeps the part already worked). Somebody who started and leaves early is replaced by hand; if nobody takes over, the planner shortens the shift.

**A deletion cannot be brought back with its attachments.** Expenses and tasks of a deleted shift are gone; the open slot is the reminder. If the absence was a mistake, plan the shift again.

**No group, no deletion.** Without a selected group nothing is deleted, also not in the dialog. If an uncovered shift unexpectedly stays, check the group selection.
