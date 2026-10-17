---
name: explain_group_hierarchy_inheritance
description: |
  Explains what carries down a group hierarchy: payment interval, calendar selection and location
  are per-group fields with no parent fallback, while visibility and filtering reach every subgroup.
  The schedule uses the group's own or the company calendar; only the proactive unstaffed-shift
  check falls back to the parent's. Covers why the hours balance (direct members) and the absence
  overlap (whole subtree) disagree. Use this when asked whether a setting cascades to subgroups.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - hierarchie
  de:
    - kaskadiert
    - vererbt sich
    - untergruppen erben
  en:
    - cascade to subgroups
    - inherit settings
synonyms:
  de: [vererbt sich das zahlungsintervall an untergruppen, gilt der kalender der gruppe auch für untergruppen, kaskadiert eine einstellung auf untergruppen, warum zeigt die stundenbilanz einer gruppe weniger leute als die absenz-überschneidung, zählt die stundenbilanz auch untergruppen mit, wirkt sich der standort einer gruppe auf untergruppen aus, übernehmen untergruppen die einstellungen der übergeordneten gruppe]
  en: [does the payment interval cascade to subgroups, does the group calendar apply to subgroups too, does a setting cascade down the group tree, why does the hours balance show fewer people than the absence overlap, does the hours balance include subgroups, does a group's location apply to its subgroups, do subgroups inherit settings from their parent group]
  fr: [l'intervalle de paiement se propage-t-il aux sous-groupes, le calendrier du groupe s'applique-t-il aux sous-groupes, un paramètre est-il hérité par les sous-groupes, le bilan d'heures inclut-il les sous-groupes]
  it: [l'intervallo di pagamento si propaga ai sottogruppi, il calendario del gruppo si applica ai sottogruppi, un'impostazione viene ereditata dai sottogruppi, il bilancio ore include i sottogruppi]
---

# Group hierarchy — what carries down and what does not

Almost nothing about a group's own configuration passes down to its subgroups automatically — only
visibility and filtering follow the tree.

<!-- level:short -->

## Subgroups do not copy their parent

Groups can sit inside other groups, like a region with teams below it. What you set on the region — for
example its payment interval — is not handed down to the teams: each team keeps its own value until you
change it there. What does follow the tree is who can see what: allowing a user to see a region also
shows every team inside it, and choosing a region at the top of the app shows all its teams together.
Example: you set the region to "Biweekly", but a team below it still says "Monthly" until you change the
team itself.

<!-- level:elements -->

## Setting values per group

1. Open the Groups page and click the pencil of the group you want to change (in the tree or in the
   table).
2. In the card (de: "Gruppe Information", en: "Group Information", fr: "Informations sur le groupe", it:
   "Informazioni sul Gruppo") set the dropdown (de: "Zahlungsintervall", en: "Payment Interval", fr:
   "Intervalle de paiement", it: "Intervallo di pagamento") and the dropdown (de: "Kalenderauswahl", en:
   "Calendar Selection", fr: "Sélection du calendrier", it: "Selezione calendario").
3. Repeat for every subgroup that should have the same value — nothing is copied for you.
4. A group's location has no input field there; it is set through Klacksy (`set_group_location`).
5. To make a user see a whole region, tick only its top-level group on the page (de: "Sichtbarkeit von
   Gruppen pro Benutzer", en: "Visibility of Groups per User", fr: "Visibilité des groupes par
   utilisateur", it: "Visibilità dei gruppi per utente"); the subgroups follow automatically.

<!-- level:effects -->

## What reaches into subgroups

### Plain values on one group

A group's payment interval, its calendar selection and its location are plain values on that one group.
Setting the payment interval on a root group to "Biweekly" has no effect on a subgroup that still says
"Monthly" — each group carries its own value. The same holds for the coordinates: a subgroup without
its own value simply has none, it does not borrow its parent's.

### Visibility and filtering

Visibility and filtering are the exception. A user's visibility is granted per top-level group and covers
that group's whole tree. Picking a group in the header selector or in a people list also includes every
subgroup underneath it, and the schedule filtered to a group shows the shifts assigned to any team under
it. For configuration the rule is "ask this group only"; for visibility it is "ask this group and
everything below it."

### Related skills

- `get_group_hours_balance` — direct members of the named group only
- `get_group_absence_overlap` — the named group plus all of its subgroups

### Trigger phrases

- "Vererbt sich das Zahlungsintervall an die Untergruppen?"
- "Gilt die Kalenderauswahl der Wurzelgruppe auch für die Teams darunter?"
- "Why does the hours balance for this group report no members?"
- "Warum zeigt die Stunden-Bilanz weniger Leute als die Absenz-Überschneidung?"

<!-- level:pitfalls -->

## Where the pattern breaks

**Two evaluations of the same group disagree.** A group's hours balance is worked out only from the
employees directly assigned to that exact group, while the absence overlap for the same group also draws
in everyone from its subgroups. Ask for the hours balance of a region that has no direct members of its
own — only teams underneath it — and it reports that the group has no members to balance, even though the
absence overlap for that same region lists everyone in it. It is worth knowing before comparing their
numbers side by side.

**The calendar falls back differently than visibility.** The schedule uses the selected group's own
calendar, otherwise the company-wide one — never the parent's. Only the proactive check for unstaffed
shifts lets a subgroup without a calendar use its parent's.

**A subgroup cannot be made visible on its own.** Visibility is granted per top-level group; a user either
sees the whole tree below it or none of it. The same goes for Klacksy: `set_user_group_scope` accepts
top-level groups only.

**The membership window is read per exact group.** While you view a parent group, the dates that lock
cells in the schedule come only from memberships in that exact group; a membership in one of its subgroups
does not lock cells there.
