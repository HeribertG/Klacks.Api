---
name: explain_group_hierarchy_inheritance
description: |
  Explains what actually carries down a group hierarchy and what does not: payment interval,
  calendar selection and location are per-group fields with no parent fallback, while visibility and
  filtering reach into every subgroup. Covers a case where two similar evaluations disagree because
  of this: one counts only direct members, the other the whole subtree. Use this when asked whether
  a setting cascades to subgroups, or why an evaluation looks smaller than expected.
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

## Core idea (one sentence)

Almost nothing about a group's own configuration passes down to its subgroups automatically — only
visibility and filtering follow the tree.

## What does not inherit

A group's payment interval, its calendar selection and its location are plain values on that one
group; there is no lookup that falls back to a parent when they are empty. Setting the payment
interval on a root group to "Biweekly" has no effect on a subgroup that still says "Monthly" — each
group carries its own value, full stop. The same holds for the calendar selection and for the
coordinates: a subgroup without its own value simply has none, it does not borrow its parent's.

## What does inherit

Visibility and filtering are the exception. Picking a group anywhere in the app — the header
selector, a person's visibility scope, the schedule filter — always reaches into every subgroup
underneath it as well. Someone allowed to see a region also sees every team inside that region; the
schedule filtered to a location shows the shifts of every team under that location too. For
configuration the rule is "ask this group only"; for visibility it is "ask this group and everything
below it."

## Where the pattern breaks

Two evaluations that ask what sounds like the same question about the same group answer it
differently. A group's hours balance is worked out only from the people directly assigned to that
exact group, while the absence overlap for the same group also draws in everyone from its subgroups.
Ask for the hours balance of a region that has no direct members of its own — only teams underneath
it — and it reports that the group has no members to balance, even though the absence overlap for
that same region correctly lists everyone in it. This is simply how the two currently differ, and it
is worth knowing before comparing their numbers side by side.

## Related skills

- `get_group_hours_balance` — direct members of the named group only
- `get_group_absence_overlap` — the named group plus all of its subgroups

## Trigger phrases

- "Vererbt sich das Zahlungsintervall an die Untergruppen?"
- "Gilt die Kalenderauswahl der Wurzelgruppe auch für die Teams darunter?"
- "Why does the hours balance for this group report no members?"
- "Warum zeigt die Stunden-Bilanz weniger Leute als die Absenz-Überschneidung?"
