---
name: explain_group_membership_validity
description: |
  Explains that a group membership has its own start and optional end date that decides who counts:
  with a group selected in the schedule, days outside a person's window are locked for them, and the
  group's hours balance counts only memberships overlapping the period. The planning wizards do not
  filter candidates by it. Ending a membership is refused while the person has planned work in that
  group afterwards. Use this when a person cannot be booked in a group or a membership cannot be
  ended.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - gruppenmitgliedschaft
  de:
    - gültigkeit der gruppenmitgliedschaft
    - gültigkeitsfenster der mitgliedschaft
  en:
    - group membership validity
    - membership validity window
synonyms:
  de: [warum kann ich diese person im plan der gruppe nicht eintragen, zählt eine person schon mit die erst nächste woche im team ist, kann ich eine gruppenmitgliedschaft rückwirkend beenden, warum lässt sich die mitgliedschaft nicht beenden, was passiert mit geplanten diensten wenn ich die mitgliedschaft beende, steuert die gültigkeit der mitgliedschaft die planung]
  en: [why can't i book this person in the group's schedule, does someone joining next week already count this period, can i end a group membership retroactively, why can't i close this membership, what happens to planned shifts if i end a membership, does membership validity control who gets scheduled]
  fr: [pourquoi je ne peux pas planifier cette personne dans ce groupe, puis-je mettre fin à une affiliation de groupe rétroactivement, que se passe-t-il avec les services déjà planifiés, la validité de l'affiliation contrôle-t-elle la planification]
  it: [perché non posso pianificare questa persona in questo gruppo, posso terminare un'appartenenza di gruppo retroattivamente, cosa succede ai turni già pianificati, la validità dell'appartenenza controlla la pianificazione]
---

# Group membership validity — not just history

A person's link to a group has its own start date and, optionally, an end date, and that window decides
whether the person can be booked in that group's schedule and whether they count in the group's numbers
for a period — not only how the past is kept intact.

<!-- level:short -->

## Joining a group for a limited time

When somebody joins a group you can give the membership a start date and an end date. Example: a seasonal
helper joins the Bern team from 1 June to 31 August. In the Bern schedule the days in May and September
are locked for that person, so nobody plans them by mistake. If you leave both dates empty, the person is a member without time limit.

<!-- level:elements -->

## Setting and ending a membership window

1. Open the person's own page and go to the card (de: "Zugewiesene Gruppen", en: "Assigned Groups", fr:
   "Groupes assignés", it: "Gruppi assegnati").
2. Click (de: "Gruppe hinzufügen", en: "Add Group", fr: "Ajouter un groupe", it: "Aggiungi gruppo") and pick
   the group. The start column is preset to today.
3. Fill or change the columns (de: "GÜLTIG VON", en: "VALID FROM", fr: "VALIDE DÈS", it: "VALIDO DA") and
   (de: "GÜLTIG BIS", en: "VALID UNTIL", fr: "VALIDE JUSQU'À", it: "VALIDO FINO A"). An empty end means no
   end.
4. Save the person. Ending a membership can be refused — see the consequences below.
5. Adding a person in the group editor, card (de: "Teilnehmerliste", en: "Participant List", fr: "Liste
   des participants", it: "Elenco dei Partecipanti"), with the button (de: "Übernehmen", en: "Apply", fr:
   "Appliquer", it: "Applica") creates a membership without any dates.
6. To see the effect, select the group at the top of the application and open the schedule: the days
   outside the person's window are locked in their row.

<!-- level:effects -->

## What the validity window drives

### The schedule

While a group is selected in the schedule, the days outside a person's membership window in that group are locked: no
shift or absence can be placed there by menu, text entry, drag and drop, fill handle or paste. This is
separate from the person's entry and exit dates (card de: "Registration", en: "Registration", fr:
"Inscription", it: "Registrazione"), which lock days in the same way.

### The group's numbers

The hours balance of a group for a chosen period counts only the employees whose membership window
overlaps that period. Join a team the week after the period closes and the period's balance simply does
not include that person yet.

### Example

A membership valid from 1 September has no place in the August schedule of that group — those days are
locked for the person. From September onward it is a completely ordinary row.

### Related skills

- `add_client_to_group_by_name` / `remove_client_from_group` — changing who is in a group
- `get_group_hours_balance` — counts only memberships valid for the requested period

### Trigger phrases

- "Warum kann ich diese Person im Plan nicht eintragen?"
- "Zählt jemand, der erst nächste Woche eintritt, schon in der Stunden-Bilanz?"
- "Warum kann ich diese Mitgliedschaft nicht beenden?"
- "What happens to planned shifts if I end someone's group membership?"

<!-- level:pitfalls -->

## Consequences and edge cases

**Ending a membership is checked against planned work.** Saving a person with an end date is refused if
they already have planned work in a shift of that group on a day after that end date (the check is strict:
the end date itself is fine). Removing the person from the group (for example in the participant list) is refused if they have any
planned work in that group's shifts, whatever the date. The messages read: "This membership cannot be
ended on this date because the employee still has planned work entries in this group afterwards." and
"This employee cannot be removed from the group because they have planned work entries in this group."
The way out is to deal with those entries first or, for an end date, to move it.

- Only the exact group is checked, not its subgroups, and only real entries — entries inside scenarios
  do not count.
- The entry and exit dates of a person are a different thing from a group window; the skills
  `update_membership` and `end_client_membership` change those entry and exit dates, not a group window,
  and carry no such check.

**The window is not a candidate filter for the planning wizards.** The wizards started from chat take the
people who have a membership entry in the group, whatever its dates; the schedule then refuses the
placements that fall outside the window.

**The window is read per exact group.** While a parent group is selected, the lock dates come only from
memberships in that exact group, not from its subgroups.
