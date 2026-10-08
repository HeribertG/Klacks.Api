---
name: explain_payroll_export_handoff
description: |
  Explains how the payroll figures of a sealed period reach the payroll system: sealing starts no
  export; an administrator runs the payroll export per employee on the period-closing page once every
  day is locked and every entry closed. Only new or changed persons are exported, so corrected figures
  after a reopen yield a supplementary export. Use this when sealing a period produced no payroll file,
  or corrected figures need to reach payroll after reopening.
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

# Payroll export — how the figures of a sealed period reach payroll

## Core idea (one sentence)

Sealing a period never produces a payroll file by itself; an administrator exports the payroll
figures afterwards, per employee, from the period-closing page.

## Why no file appeared after sealing

1. **Sealing starts no export.** Closing a period only locks the days and writes the audit entry.
   There is no automatic handover, whatever group the period was sealed for.
2. **The export is a separate step on the period-closing page** (exports area, employee export). It
   is administrator-only and uses the installation-wide payroll settings, not a per-group setup.
3. **The export is blocked while the period is incomplete.** Every day with an entry (and every day
   a person is an active group member) must be locked by a period close, and every entry must be
   closed. The page lists each blocker per person and day, and says when a global close is needed
   because no group covers the day.
4. **An earlier export of the same person blocks an overlapping period.** A person exported for
   another period that overlaps the chosen one has to be handled first.

## Corrected figures after a reopen

Each person is exported only when their figures are new or changed since the last export of the same
period and format. After reopening, correcting and sealing again, the next export contains just the
changed persons and is marked as a **supplementary export**; if nothing changed, the page says that
nothing is new. Earlier exports stay in the export history and can be downloaded again at any time.

## Practical guidance

After reopening and correcting a period, seal it again, open the employee export on the
period-closing page, check the preview (blockers, new or changed persons) and run the export.

## Related skills

`list_open_periods`, `close_period`, `reopen_period`, `list_sealed_orders`, `list_recent_exports`

## Trigger phrases

- "Warum fehlt die Lohndatei nach dem Versiegeln?"
- "I reopened and corrected the period but the payroll file still has the old numbers."
- "Wie bekomme ich korrigierte Zahlen nach dem Wiedereröffnen ins Lohnsystem?"