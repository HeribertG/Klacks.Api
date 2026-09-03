---
name: explain_group_model
description: |
  Explains what groups are for beyond folders: they carry the visibility scope that decides which
  people a user sees at all, they can be filled automatically by criteria such as region, contract or
  qualification, and they can be matched geographically so customers and staff land in the group
  nearest to them. Use this when the user asks why somebody cannot see certain people, how to fill a
  group without picking each person, or what a group's location is for.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - sichtbarkeit
  - visibility
  - sieht nicht alle
  - gruppe befüllen
  - nächste gruppe
  - standort
synonyms:
  de: [sichtbarkeit, sichtbarkeitsbereich, sieht nicht alle mitarbeiter, gruppe automatisch befüllen, nach kriterien, nächstgelegene gruppe, standort der gruppe, geokodierung]
  en: [visibility, visibility scope, cannot see all employees, fill a group automatically, by criteria, nearest group, group location, geocoding]
  fr: [visibilité, portée de visibilité, ne voit pas tous les employés, remplir un groupe automatiquement, groupe le plus proche]
  it: [visibilità, ambito di visibilità, non vede tutti i dipendenti, riempire un gruppo automaticamente, gruppo più vicino]
---

# Groups — more than folders

## Core idea (one sentence)

A group is not only a way to sort people; it is what decides who a user is allowed to see and where
work is geographically anchored.

## Visibility

Every user can be given a **visibility scope**: a set of groups. From then on they see only the
people in those groups — across the whole application, not just in one list. Somebody reporting that
"half the staff are missing" usually has a scope set, not a broken filter.

Two things about it are worth knowing: setting a scope **replaces** whatever was there before rather
than adding to it, and the group selection at the top of the application works inside that scope,
never around it.

## Filling a group without picking people

A group can be filled in one step from criteria instead of person by person: region or canton, town,
the start of a postal code, an active contract by name, a qualification by name, or the kind of
person. That is how "all full-time staff in the Bern area with a forklift licence" becomes a group
without a list being clicked together by hand.

There is also a proposal step: Klacks can suggest a grouping first and apply it only after it has
been looked at.

## Groups have a place

A group can carry a **location**. Once locations are known, people and customers can be assigned to
the group **nearest to their own address** — computed from the addresses, not guessed from names.
This is what makes regional structures maintainable when staff move or new customers arrive.

Because this depends on coordinates, there is a way to check which groups still lack them before
relying on the assignment.

## Structure and time

Groups form a tree and can be moved within it. Membership is not permanent by nature: it has a
validity period, so somebody can belong to a group for a season without their history being
rewritten afterwards.

## Groups answer questions too

Because a group bundles people, it is also a unit for evaluation — the hours balance of all its
members for a period, sorted from most under-target to most overtime, or the absence overlap within
it.

## Related skills

- `list_groups` / `list_groups_hierarchical` / `get_group_details` / `list_group_members`
- `create_group` / `update_group` / `move_group` / `delete_group`
- `set_user_group_scope` / `get_user_group_scope` — who sees which groups
- `fill_group_by_criteria` / `propose_grouping` / `apply_grouping` — filling without picking
- `partition_clients_by_address` — builds a whole canton/city group tree from addresses in one call,
  for a fresh install that has clients but no groups yet
- `set_group_location` / `add_client_to_nearest_group` / `check_group_geocoding_status` — geography
- `get_group_hours_balance` / `get_group_absence_overlap` — evaluation

## Trigger phrases

- "Why does this user not see all the staff?"
- "Put every full-time employee in the Bern region into this group."
- "What is a group's location good for?"
- "Who in this group has overtime?"
- "Warum sehe ich nur einen Teil der Mitarbeiter?"
