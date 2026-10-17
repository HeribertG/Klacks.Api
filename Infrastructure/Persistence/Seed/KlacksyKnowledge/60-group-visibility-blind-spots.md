---
name: explain_group_visibility_blind_spots
description: |
  Explains two surprises of group visibility. Once any group exists, a non-admin user with no
  top-level group ticked is not unrestricted: they see only people who belong to no group, and the
  dashboard shows them no data; only administrators are unrestricted. And a person with no group
  membership stays visible to every restricted user, so leaving someone group-less never hides them.
  Use this when asked what an unconfigured scope shows or why a group-less person still appears.
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
  de: [was sieht ein benutzer ohne sichtbarkeitsbereich, sieht ein benutzer ohne konfigurierte sichtbarkeit alles, warum sehe ich personen ohne gruppe trotzdem, bleiben gruppenlose mitarbeiter für eingeschränkte benutzer sichtbar, was passiert wenn ich keine sichtbarkeit für einen benutzer einstelle, warum sieht ein neuer benutzer fast niemanden]
  en: [what does a user see with no visibility scope configured, does an unconfigured visibility scope show everything, why can i still see people who are in no group at all, do group-less employees stay visible to restricted users, what happens if i never set a visibility scope for a user, why does a new user see almost nobody]
  fr: [que voit un utilisateur sans portée de visibilité configurée, les employés sans groupe restent-ils visibles, que se passe-t-il si je ne configure aucune visibilité, pourquoi un nouvel utilisateur ne voit presque personne]
  it: [cosa vede un utente senza ambito di visibilità configurato, i dipendenti senza gruppo restano visibili, cosa succede se non configuro alcuna visibilità, perché un nuovo utente non vede quasi nessuno]
---

# Group visibility — what it hides and what it does not

A visibility scope narrows what a user sees to the groups an administrator has ticked for them — but
people who belong to no group at all stay visible to every restricted user.

<!-- level:short -->

## Who sees which people

Not every user should see every person. An administrator therefore decides, for each user, which groups
they may see. From then on that user sees only the people of those groups — in the people lists, in the
schedule and in the dashboard. Example: a planner who is allowed to see only the Bern group never sees
the staff of the Zurich group. Administrators are never restricted: they always see everything.

<!-- level:elements -->

## Setting who sees which groups

1. Open the page `/workplace/group-structure`, headed (de: "Sichtbarkeit von Gruppen pro Benutzer", en:
   "Visibility of Groups per User", fr: "Visibilité des groupes par utilisateur", it: "Visibilità dei
   gruppi per utente"). There is one row per user account.
2. Click the counter button of the user in the column (de: "Anzahl", en: "Number"). For an administrator
   account the button is disabled.
3. In the dialog (de: "Sichtbare Rootgruppe", en: "Visible Root Group", fr: "Groupe racine visible", it:
   "Gruppo radice visibile") tick the top-level groups the user may see. Only top-level groups are
   offered; their subgroups follow automatically.
4. Klacksy can do the same with `set_user_group_scope` (it replaces the user's previous selection and
   refuses administrator accounts).

<!-- level:effects -->

## What a restricted user sees

Visibility for a user is a list of top-level groups. The user sees the people who belong to at least one
group inside those trees, plus the people who belong to no group at all. The same limit applies to the
schedule's people, the dashboard widgets (staff map, coverage statistics, resource monitor), reading and
changing data, and Klacksy's skills: a person or group outside the scope is answered as if it did not
exist. The group selection at the top of the application works inside the scope, never around it.

Administrators are unrestricted, and so is everybody on an installation that has no group at all.

### Related skills

- `set_user_group_scope` / `get_user_group_scope`

### Trigger phrases

- "Ich habe für diesen Benutzer noch keine Sichtbarkeit eingestellt — was sieht er?"
- "Warum sieht dieser Mitarbeiter ohne Gruppe trotzdem jeder?"
- "Why is the dashboard empty for this user?"
- "Kann ich jemanden verstecken, indem ich sie in keine Gruppe stecke?"

<!-- level:pitfalls -->

## Two blind spots

**A user with no top-level group ticked sees almost nobody.** As soon as at least one group exists, a
non-admin user without any ticked group is restricted to nothing: the people lists show only people who
belong to no group at all, and the dashboard widgets show no data (Klacksy's dashboard overview says the
dashboard is empty for that user). Somebody given a fresh login therefore sees an almost empty
application until an administrator ticks groups for them. Only on an installation without any group is
every user unrestricted.

**People in no group are always visible.** A restricted scope filters by group membership — but a person
with no membership entry at all is never filtered out. Hiding someone from a restricted user by leaving
them in no group does not work; the opposite happens, they stay visible to everyone. To keep somebody out
of a restricted user's view, they have to be inside a group that lies outside that user's visible
groups.

- The test is "has any membership entry", without looking at dates: a person whose membership has
  expired still counts as being in a group and is hidden like any other member.
- A person in several groups is visible as soon as one of those groups is visible, so one visible group
  is enough to expose them.
- Removing somebody's last membership makes them group-less and thereby visible to all restricted users;
  the app does not protect against that.
