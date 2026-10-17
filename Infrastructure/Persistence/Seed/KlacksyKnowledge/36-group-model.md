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

A group is not only a way to sort people: it is the planning sheet a team is scheduled on, it decides
who a user is allowed to see, and it anchors work geographically.

<!-- level:short -->

## What groups are for

A group is a team, a location or any unit that is planned together — for example "Bern". Employees,
external employees and customers can be put into groups, and one person can belong to several groups
at the same time. Each group has its own planning sheet: choose the group at the top of the app and the
schedule shows that group's people and shifts. Groups also decide who sees whom — a user who is only
allowed to see the Bern group does not see the people of other groups. That keeps every planner's view
small and keeps staff data away from people who do not need it.

<!-- level:elements -->

## Working with groups, step by step

The Groups pages are for administrators.

1. **Create a group:** open the Groups page and click the button (de: "+ Gruppe erfassen", en: "+ Create
   Group", fr: "+ Créer un groupe", it: "+ Crea gruppo"). In the editor give the group a name; the card
   (de: "Gruppenstruktur", en: "Group Structure", fr: "Structure du groupe", it: "Struttura del Gruppo")
   has the selection (de: "Übergeordnete Gruppe auswählen", en: "Select Parent Group", fr: "Sélectionner le
   groupe parent", it: "Seleziona Gruppo Genitore") that puts it below another group.
2. **Put people into a group** — two ways:
   - In the group editor, card (de: "Teilnehmerliste", en: "Participant List", fr: "Liste des
     participants", it: "Elenco dei Partecipanti"): search the person by name or ID number and click
     (de: "Übernehmen", en: "Apply", fr: "Appliquer", it: "Applica"). A person added this way is a member
     without start or end date.
   - On the person's own page, card (de: "Zugewiesene Gruppen", en: "Assigned Groups", fr: "Groupes
     assignés", it: "Gruppi assegnati"): click (de: "Gruppe hinzufügen", en: "Add Group", fr: "Ajouter un
     groupe", it: "Aggiungi gruppo") and, if the membership is limited in time, fill the columns (de:
     "GÜLTIG VON", en: "VALID FROM", fr: "VALIDE DÈS", it: "VALIDO DA") and (de: "GÜLTIG BIS", en: "VALID
     UNTIL", fr: "VALIDE JUSQU'À", it: "VALIDO FINO A").
3. **Decide who sees which groups:** open the page (de: "Sichtbarkeit von Gruppen pro Benutzer", en:
   "Visibility of Groups per User", fr: "Visibilité des groupes par utilisateur", it: "Visibilità dei gruppi
   per utente"), click the counter of a user in the column (de: "Anzahl", en: "Number") and tick the
   top-level groups in the dialog (de: "Sichtbare Rootgruppe", en: "Visible Root Group", fr: "Groupe racine
   visible", it: "Gruppo radice visibile"). The button of an administrator account is disabled —
   administrators always see everything.
4. **Plan one group:** pick it in the group selection at the top of the app; pick (de: "Alle Gruppen",
   en: "All groups", fr: "Tous les groupes", it: "Tutti i gruppi") to see everything the user may see.
5. **Assign a shift to a group:** in the shift's edit mask use the card (de: "Gruppe", en: "Group", fr:
   "Groupe", it: "Gruppo"); the shift filter of the schedule has the checkbox (de: "Nicht gruppierte Dienste", en: "Ungrouped
   Shifts", fr: "Services non groupés", it: "Turni non raggruppati") that includes shifts without any group.
6. **Fill a group by criteria or by location, set coordinates:** there is no input field for this on the
   Groups pages; ask Klacksy (see the skills below).

<!-- level:effects -->

## How groups affect the rest of Klacks

### Who sees whom

A non-admin user can be given a **visibility scope**: a set of top-level groups, each of which includes
everything below it. From then on the user sees only people in those groups — across the whole
application (lists, schedule, dashboard, and when changing data), not just in one list. A person is
visible as soon as at least one of their groups is visible. Administrators are never restricted.

Two things about it are worth knowing: setting a user's scope **replaces** whatever was there before
instead of adding to it, and the group selection at the top of the application works inside that scope,
never around it — a selected group is intersected with what the user may see.

### The planning sheet

The group chosen at the top filters the schedule's people (including subgroups) and the shifts assigned
to the group or its subgroups. The group's own payment interval gives the schedule its period grid; a
group without one falls back to the company setting, not to its parent.

### Shifts and groups

A shift is assigned to groups in its own edit mask. Which group a booking counts for is decided by the
group of its **shift**, not by the person's own groups — see the pitfalls for what that means when a
person works in two groups.

### Filling a group without picking people

A group can be filled in one step from criteria instead of person by person: canton or region, town,
the start of a postal code, an active contract by name, a qualification by name, and the kind of person
(employee by default). That is how "all full-time staff in the Bern area with a forklift licence"
becomes a group without a list being clicked together by hand. The first run is a preview; people are
added only when it is applied.

There is also a proposal step for regrouping by location: Klacks first shows what would change and
applies it only after confirmation.

### Groups have a place

A group can carry a **location** (coordinates). Customers and external employees can then be assigned
to the group **nearest to their own address** — computed from the addresses, not guessed from names.
This is what makes regional structures maintainable when staff move or new customers arrive. There is
a way to check which groups still lack coordinates before relying on the assignment.

### Structure and time

Groups form a tree and can be moved within it. A person's link to a group has its own validity period,
so somebody can belong to a group for a season without their history being rewritten afterwards.

### Groups answer questions too

Because a group bundles people, it is also a unit for evaluation — the hours balance of its employees
for a period, from the lowest balance (most under target) to the highest (most overtime), or the absence
overlap within it.

### Related skills

- `list_groups` / `list_groups_hierarchical` / `get_group_details` / `list_group_members`
- `create_group` / `update_group` / `move_group` / `delete_group`
- `set_user_group_scope` / `get_user_group_scope` — who sees which groups
- `fill_group_by_criteria` / `propose_grouping` / `apply_grouping` — filling without picking
- `partition_clients_by_address` — builds the whole location group tree (region → state/province →
  city cluster) from the addresses of employees, external employees and customers in one call, for a
  fresh install that has clients but no groups yet; orders then follow their customer's cluster. Level
  `cluster_municipality` adds municipality sub-groups below a city cluster, but only for larger
  accumulations; small places join the nearest sub-group or stay in the city group
- `partition_clients_by_qualification` — one group per currently valid qualification (preview first,
  then apply on confirmation); a person with several qualifications is in several groups. With
  `rootGroupName` the qualification groups are created below that group, for its members only
  (e.g. qualification groups per location)
- `evaluate_grouping_by_qualification` / `evaluate_location_group_candidates` — advice only, creates nothing
- `set_group_location` / `add_client_to_nearest_group` / `check_group_geocoding_status` — geography
- `get_group_hours_balance` / `get_group_absence_overlap` — evaluation

### Trigger phrases

- "Why does this user not see all the staff?"
- "Put every full-time employee in the Bern region into this group."
- "What is a group's location good for?"
- "Who in this group has overtime?"
- "Warum sehe ich nur einen Teil der Mitarbeiter?"

<!-- level:pitfalls -->

## Consequences and edge cases

**Deleting a group is final.** The group and all its subgroups are deleted together; only an
administrator can do it, and there is no restore function — neither in the app nor through Klacksy. The
delete also removes the memberships of every person and the assignments of every shift in the deleted
groups (subgroups included). A person without another group becomes group-less and is therefore visible to
every user with a restricted group view; in the group selection they show up again under (de: "Ohne
Gruppe", en: "Without group", fr: "Sans groupe", it: "Senza gruppo"). A shift without another group becomes
ungrouped. Memberships in other groups stay as they are.

**A booking that belongs to another group is locked.** Take a person who is a member of groups A and B.
In the view of group B you still see their booking for a shift that is assigned only to group A (outside
the group you are viewing) — it is shown so nobody gets booked twice by accident — but it is locked: the
tooltip says (de: "Gruppeneinschränkung", en: "Group restricted", fr: "Restriction de groupe", it:
"Restrizione di gruppo"), and it cannot be edited, moved or deleted from the grid there. Change it in
the view of the group that owns the shift. This only applies while a group is selected, and bookings on
shifts without any group are never locked this way.

**People without any group are visible to every restricted user.** The check is only "has no membership
entry at all", with no dates: an expired membership still counts as a group. Taking away somebody's last
membership therefore makes them visible to all restricted users, and nothing prevents that.

**Nearest group has limits.** It places customers and external employees, not employees; only groups
that carry coordinates are candidates (a group whose name is not a place is skipped); the distance is
air-line, not by road; and a person without a geocoded address is left where they are. A regrouping that
moves people can end their other memberships — the proposal states how many before anything is applied.

**Settings do not cascade down the tree.** Payment interval and location are values of that one group;
subgroups do not inherit them from their parent.

**Evaluations differ.** A group's hours balance counts only the employees directly assigned to that
exact group (and only those whose membership overlaps the period), while the absence overlap also
includes the subgroups.
