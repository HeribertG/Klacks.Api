---
name: explain_group_membership_validity
description: |
  Explains that a group membership carries its own start and optional end date, and that this window
  actively decides who counts, not only how history is kept. The automatic planner draws its
  candidate pool for a group from memberships overlapping the planning period, and a group's hours
  balance follows the same rule. Also covers that ending a membership is refused once the person
  already has planned shifts in that group afterwards. Use this when asked why the planner skipped
  somebody.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - gruppenmitgliedschaft
  de:
    - gültigkeit der gruppenmitgliedschaft
    - kandidatenkreis der planung
  en:
    - group membership validity
    - planner candidate pool
synonyms:
  de: [warum schlägt der planungs-assistent diese person nicht vor, zählt eine person schon mit die erst nächste woche im team ist, kann ich eine gruppenmitgliedschaft rückwirkend beenden, warum lässt sich die mitgliedschaft nicht beenden, was passiert mit geplanten diensten wenn ich die mitgliedschaft beende, steuert die gültigkeit der mitgliedschaft die planung]
  en: [why didn't the planner suggest this person for the team, does someone joining next week already count this period, can i end a group membership retroactively, why can't i close this membership, what happens to planned shifts if i end a membership, does membership validity control who gets scheduled]
  fr: [pourquoi l'assistant de planification n'a pas proposé cette personne, puis-je mettre fin à une affiliation de groupe rétroactivement, que se passe-t-il avec les services déjà planifiés, la validité de l'affiliation contrôle-t-elle la planification]
  it: [perché l'assistente di pianificazione non ha proposto questa persona, posso terminare un'appartenenza di gruppo retroattivamente, cosa succede ai turni già pianificati, la validità dell'appartenenza controlla la pianificazione]
---

# Group membership validity — not just history

## Core idea (one sentence)

A group membership has its own start date and, optionally, an end date, and that window actively
decides who the automatic planner considers and who a period's numbers include — not only how the
past is kept intact.

## What the validity window drives

When the automatic planner builds a work schedule for a group, its pool of schedulable people is
exactly the memberships whose window overlaps the planning period — someone whose membership starts
after the period ends, or ended before it begins, is not offered as a candidate at all, even though
they still show up in the group's plain member list. The same overlap rule decides who counts in a
group's hours balance for a chosen period: join a team the week after that period closes, and the
period's balance simply does not include that person yet.

## Example

A membership valid from 1 September has no bearing on planning in August — the planner will not
propose that person for an August roster no matter how visibly the membership already sits in the
group. From September onward it becomes a completely ordinary candidate.

## Closing a membership is checked against reality

Ending a membership — setting its last day, or removing it outright — is refused if the person
already has planned shifts in that group after the chosen end date (or, for an outright removal, any
planned shift in it at all). The membership has to be adjusted, or those shifts dealt with first;
Klacks will not silently leave a scheduled shift pointing at a membership that no longer covers that
day.

## Related skills

- `add_client_to_group_by_name` / `remove_client_from_group` — changing who is in a group
- `get_group_hours_balance` — counts only memberships valid for the requested period

## Trigger phrases

- "Warum hat der Planungs-Assistent diese Person nicht vorgeschlagen?"
- "Zählt jemand, der erst nächste Woche eintritt, schon in der Stunden-Bilanz?"
- "Warum kann ich diese Mitgliedschaft nicht beenden?"
- "What happens to planned shifts if I end someone's group membership?"
