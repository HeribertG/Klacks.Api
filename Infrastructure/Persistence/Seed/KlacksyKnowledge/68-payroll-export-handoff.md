---
name: explain_payroll_export_handoff
description: |
  Explains why the automatic handover of a sealed, group-scoped period's payroll figures can
  silently produce nothing: a separate country add-on must be installed and active, the handover
  runs only once per group, target system and exact date range, and a manual download for that
  range counts as that handover too. Use this when sealing a period produced no payroll file, or
  corrected figures need to reach payroll after reopening.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - payroll
  de:
    - lohndatei fehlt
    - lohnübergabe einmalig
    - zusatzpaket lohnexport
  en:
    - missing payroll file
    - payroll handover once
    - payroll add-on
synonyms:
  de: [warum fehlt die lohndatei nach dem versiegeln, lohndatei nach wiedereröffnung noch alt, zusatzpaket für lohnexport nicht aktiv, lohnübergabe nur einmal pro periode, korrigierte zahlen nach wiedereröffnung ins lohnsystem]
  en: [why is the payroll file missing after sealing, payroll file still shows old numbers after reopening, payroll export add-on not active, payroll handover only happens once per period, get corrected figures into payroll after reopening]
  fr: [pourquoi le fichier de paie manque après le scellement, module de paie non actif, le transfert de paie ne se fait qu'une fois, obtenir les chiffres corrigés après réouverture]
  it: [perché manca il file paghe dopo la sigillatura, componente aggiuntivo paghe non attivo, il trasferimento paghe avviene una sola volta, ottenere i dati corretti dopo la riapertura]
---

# Payroll export handoff — why the automatic handover can go silent

## Core idea (one sentence)

Sealing a group-scoped period can hand its payroll figures to an outside system automatically, but
three separate, independent safeguards each have to agree before that actually happens.

## The three safeguards

1. **A separate add-on for the target country/system must be installed and switched on.** If it is
   not, the handover produces absolutely nothing — no file, no note anywhere. This is stricter than
   an ordinary disabled export format, which at least leaves a note in the log; here there is none.
2. **The handover runs once per group, target system and exact date range.** Reopen a sealed
   period, correct something and seal it again, and the second sealing is skipped without comment
   — a record of the first handover already exists for that exact combination, and reopening does
   not remove it.
3. **A manual, on-demand payroll download for that same group/system/range writes the same kind of
   record.** A manual download performed before an automatic one "uses up" the automatic handover
   for that period just as effectively as an earlier automatic one would.

| Reason nothing (new) arrived | Leaves a note in the log? | Blocks a later manual download? |
|---|---|---|
| Add-on not installed/active | no | no |
| Already handed over for this exact group/system/range | yes (the earlier entry) | no |

## Two precision points

- **The manual download does not apply a saved correction overlay** — it always uses the defaults,
  even when a correction for that target system is active and would apply to the automatic path.
- **The one-time lock is exact.** It only fires on an identical group, target system and start/end
  date. A different range, or a different target system, is not blocked by an earlier export.

## Practical guidance

After reopening and correcting a sealed, group-scoped period, the reliable way to get the corrected
figures into the payroll system is the **manual export** in the exports area — it is not subject to
either the add-on gate or the one-time lock the automatic handover carries. It will, however, not
pick up a saved correction overlay either, so check the figures against any active correction by
hand.

## Related skills

`list_open_periods`, `close_period`, `reopen_period`, `list_sealed_orders`, `list_recent_exports`

## Trigger phrases

- "Warum fehlt die Lohndatei nach dem Versiegeln?"
- "I reopened and corrected the period but the payroll file still has the old numbers."
- "Wie bekomme ich korrigierte Zahlen nach dem Wiedereröffnen ins Lohnsystem?"
