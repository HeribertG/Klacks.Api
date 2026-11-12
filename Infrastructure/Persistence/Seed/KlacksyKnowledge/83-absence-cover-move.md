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

1. **In the schedule:** right-click the shift of the absent person and choose (de: "Ausfall decken...", en: "Cover absence...", fr: "Couvrir l'absence...", it: "Copri assenza..."). The dialog (de: "Ausfall decken", en: "Cover absence", fr: "Couvrir une absence", it: "Copri assenza") books the absence and proposes people who can take the shift. Its result is a scenario.
2. **Accept the scenario.** Only then does the move become real: a fully covered shift moves to the replacement, an uncovered shift that has not started is deleted.
3. **Absence entered directly:** booking an absence over a day with shifts first shows (de: "Dienste werden gelöscht", en: "Services will be deleted", fr: "Des services seront supprimés", it: "I turni verranno eliminati") with the shifts concerned. Confirming with (de: "Absenz eintragen", en: "Enter absence", fr: "Saisir l'absence", it: "Inserisci assenza") books the absence and deletes those shifts at once.
4. **Check the error list.** A shift that stays in place next to an absence is reported as (de: "Absenz nicht gedeckt: {{absence}}, Dienst {{shifts}} bleibt ohne Ablösung bestehen", en: "Absence not covered: {{absence}}, shift {{shifts}} still stands without a replacement"). Delete or shorten it by hand, or cover it.

<!-- level:effects -->

## What moves, what is deleted, what stays

### Who covers: a free person or a short chain

Klacks first looks for a person who can take the shift as it stands: on call that day, then free, in the group first, then from a sister group under the same root group (borrowed for that day). If nobody can, it tries a chain of at most three moves: a qualified person takes the open shift and hands one of their own shifts to someone else, who may hand one on once more. The shift handed on can lie on the same day (the holder of the early takes the open late and gives the early away) or within seven days before or after the absence day, never in the past; this is how a person starts a block one day earlier and gives its last day away. A person from a sister group only ever ends a chain: their own shifts in the other group are never touched. Fewer moves always win; three moves are only proposed when nothing shorter is allowed.

Every move keeps the hard rules: qualification, availability, rest time, days in a row, daily, weekly and period hours. Late before early or night before late is allowed once the rest time between the two shifts is kept; such a person only ranks behind one whose rhythm fits. The dialog lists every move of a chain under the covered shift, so everybody concerned can be asked. If a later rule check refuses the proposed person, Klacks takes the next candidate instead of leaving the shift open.

### Alternatives: the same people, marked

The alternatives for a covered shift come from the same pool as the automatic proposal: the members of the group first, then people from sister groups under the same root group, marked as borrowed. A borrowed person is also checked against their own plan, so a shift or an absence in their home group excludes them. A person whose contract lists that weekday as a day off is shown and marked, not excluded; ask them before you choose them. The automatic proposal never takes a person on their contract day off. When a borrowed person is accepted as the alternative, they join the group in the scenario for that day. Accepted on two separate days, the same person is a member for the whole span between those days, and taking the scenario over makes that span a real membership.

Asked in the chat who could take a shift, Klacksy names the ten best alternatives and gives the rest as numbers: how many more could take it, and how many are excluded for which reason, in the group and among the borrowed people. The dialog shows the full list.

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
- "Warum muss Tom seinen Dienst abgeben, wenn Elsa krank ist?"
- "Can Klacks swap shifts to cover an absence?"
- "Why does the list of alternatives show somebody from another group?"
- "Warum steht bei einer Alternative, dass die Person frei hat?"

<!-- level:pitfalls -->

## Consequences and edge cases

**The route decides between moving and replacing.** The planner's dialog always treats the person as not having started, so a full cover moves the shift. Through Klacksy or a messenger, the shift moves only when the absence was reported before the shift starts; reported later, the cover becomes a replacement (the original person keeps the part already worked). Somebody who started and leaves early is replaced by hand; if nobody takes over, the planner shortens the shift.

**A deletion cannot be brought back with its attachments.** Expenses and tasks of a deleted shift are gone; the open slot is the reminder. If the absence was a mistake, plan the shift again.

**A chain is one proposal.** It only works when every person in it agrees. Accept the scenario once all of them agreed; otherwise reject it and cover again. A chain can hand a shift on, on another day than the absence: the scenario then spans that day as well, and accepting it writes the plan of the group for that day from the scenario. When another person takes the covered shift instead (an accepted alternative in the dialog), Klacks takes the chain of the previous person back: the shifts handed on return to their people, on other days too. If that is not clear-cut, because one of those people covers something else in the same scenario or a shift can no longer return, the change is refused; then reject the scenario and cover the absence again.

**No group, no deletion.** Without a selected group nothing is deleted, also not in the dialog. If an uncovered shift unexpectedly stays, check the group selection.

**A stale cover is covered again, not forced.** A cover built on the current plan can only be accepted as long as the plan in its range has not changed since. If it has (another cover accepted, a shift planned by hand, an absence entered), the warning cannot be overridden, neither in the dialog nor through Klacksy: reject the cover and cover the absence again; the refusal names the person and the days of the absence to cover. The same holds for every scenario built on such a cover (a further cover on top of it, a planning run or Harmonizer applied on it): building on a stale cover does not make it overridable. A cover built on an autofill or other planning scenario keeps the ordinary warning, which can be overridden.

**An accept never deletes an absence.** This holds for every scenario, also autofill and wizard proposals: if the scenario does not carry an absence of the plan (same person, same day, same type), for example because it was calculated before the absence was entered, it cannot be accepted, and no override changes that. The message names the missing absences; calculate the scenario again on the current plan.

**Several sick people at once (flu wave).** Two orders work cleanly: (1) cover the first absence and accept it right away, then cover the next one, and so on; or (2) cover the next absence on top of the open cover proposal (the new cover builds on that scenario) and accept only the last one, which carries all of them. Covering on top of each other works as long as the plan has not changed since the first cover of the chain; do not accept a cover in the middle of the chain first, because that changes the plan and the covers built on it have to be covered again. Several independent covers made on the current plan do not add up: once the first one is accepted, the others are out of date and have to be covered again.
