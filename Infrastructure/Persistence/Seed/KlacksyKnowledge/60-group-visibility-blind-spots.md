---
name: explain_group_visibility_blind_spots
description: |
  Explains two behaviours of a group visibility scope that contradict what the name suggests. With
  no root group configured for a user, most of the app treats them as unrestricted rather than
  showing nothing — though the dashboard widgets do the opposite and show no data until a scope is
  set. Separately, a person with no active group membership stays visible to every restricted user.
  Use this when asked what an unconfigured scope shows, or why a group-less person still appears.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - sichtbarkeitsbereich
  de:
    - keine sichtbarkeit eingestellt
    - gruppenlos trotzdem sichtbar
  en:
    - no visibility scope configured
    - group-less still visible
synonyms:
  de: [was sieht ein benutzer ohne sichtbarkeitsbereich, sieht ein benutzer ohne konfigurierte sichtbarkeit alles, warum sehe ich personen ohne gruppe trotzdem, bleiben gruppenlose mitarbeiter für eingeschränkte benutzer sichtbar, was passiert wenn ich keine sichtbarkeit für einen benutzer einstelle, warum ist das dashboard leer aber die einsatzplanung nicht]
  en: [what does a user see with no visibility scope configured, does an unconfigured visibility scope show everything, why can i still see people who are in no group at all, do group-less employees stay visible to restricted users, what happens if i never set a visibility scope for a user, why is the dashboard empty but the schedule is not]
  fr: [que voit un utilisateur sans portée de visibilité configurée, les employés sans groupe restent-ils visibles, que se passe-t-il si je ne configure aucune visibilité, pourquoi le tableau de bord est-il vide mais pas le planning]
  it: [cosa vede un utente senza ambito di visibilità configurato, i dipendenti senza gruppo restano visibili, cosa succede se non configuro alcuna visibilità, perché la dashboard è vuota ma la pianificazione no]
---

# Group visibility — two blind spots

## Core idea (one sentence)

A restricted visibility scope only ever narrows what somebody sees once it has actually been set —
before that, and for people in no group at all, the default leans towards showing rather than
hiding.

## No scope set means no restriction — almost everywhere

Visibility for a user is a list of root groups they are allowed to see. As long as that list is
empty, the assistant, the employee list and the schedule all treat the user like an unrestricted
one: an unconfigured scope is read as "not restricted yet," not as "restricted to nothing."
Somebody given a fresh login who never gets a root group ticked for them at
`/workplace/group-structure` therefore sees everything, not nothing.

That default is not universal, though. The dashboard's own overview widgets — the staff map, the
coverage statistics, the resource monitor — work the other way round: for a non-admin user with no
root group configured yet, they show no data at all instead of falling back to unrestricted. So the
very same account can see the full staff list and the full schedule while its dashboard sits empty —
worth knowing before concluding the dashboard is broken.

## People in no group are always visible

A restricted scope filters by group membership — but a person carrying no active group membership at
all is never filtered out by it. Trying to hide someone from a restricted user by simply not putting
them in any group does not work; the opposite happens, they stay visible regardless of scope. To
actually keep somebody out of a restricted user's view, they need to be inside a group that itself
lies outside that user's visible scope — being in no group at all is not a way to hide.

## Related skills

- `set_user_group_scope` / `get_user_group_scope`

## Trigger phrases

- "Ich habe für diesen Benutzer noch keine Sichtbarkeit eingestellt — was sieht er?"
- "Warum sieht dieser Mitarbeiter ohne Gruppe trotzdem jeder?"
- "Why is the dashboard empty for this user but the schedule isn't?"
- "Kann ich jemanden verstecken, indem ich sie in keine Gruppe stecke?"
