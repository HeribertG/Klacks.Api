---
name: explain_active_industries
description: |
  Explains the setting that narrows the pick lists offered when staffing: choosing a sector such as
  home care, healthcare, security, facility services or logistics shortens the choices of working
  qualifications on a shift and on a person, and of the rule preset attached to working conditions.
  Covers what the free choice does, that entries without a sector always stay available, and that
  nothing is ever deleted or hidden from the administration lists. Use this when the user asks why
  a qualification is missing from a dropdown, or what happens when the sector is switched.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - branche
    - aktive branchen
    - spitex
    - qualifikation fehlt in der auswahl
  en:
    - active industries
    - sector
    - industry filter
synonyms:
  de: [branche, aktive branchen, branchenauswahl, spitex, pflege zu hause, gesundheitswesen, sicherheitsdienst, logistik, warum fehlt die qualifikation in der liste, auswahl ist zu kurz]
  en: [active industries, sector, industry, home care, healthcare, security, facility services, logistics, qualification missing from dropdown, list too short]
  fr: [secteurs actifs, secteur, soins à domicile, santé, sécurité, logistique, qualification manquante dans la liste]
  it: [settori attivi, settore, assistenza domiciliare, sanità, sicurezza, logistica, qualifica mancante nell elenco]
---

# Sector — what it narrows, and what it leaves alone

## Core idea (one sentence)

Choosing a sector (de: "Aktive Branchen", en: "Active industries", fr: "Secteurs actifs",
it: "Settori attivi") shortens three pick lists so that staff see only what fits their line of
work — it never removes data.

## The choice

Card anchor: `settings-active-industries`. Exactly one sector is chosen, not several:

- **Home care** (de: "Spitex/Pflege zu Hause", en: "Home care", fr: "Soins à domicile",
  it: "Assistenza domiciliare")
- **Healthcare** (de: "Gesundheitswesen")
- **Security** (de: "Sicherheit")
- **Facility services** (de: "Facility Services")
- **Logistics** (de: "Logistik")
- **Free choice** (de: "Eigene Auswahl") — the fifth option, meaning no preset sector

The card saves on its own; there is no save button.

Three states are possible and they differ:

| Setting | Effect on the pick lists |
|---|---|
| nothing chosen | no narrowing at all — everything is offered |
| a sector | entries of that sector, plus every entry that carries no sector |
| free choice | only entries that carry no sector |

## What it actually narrows

Exactly three places:

1. the working qualifications offered on a shift
2. the working qualifications offered on a person
3. the rule preset that can be attached to a set of working conditions

**And nothing else.** The administration lists in the settings themselves stay complete — every
qualification and every rule preset remains visible, editable and deletable there. Nothing is
deleted, archived or hidden, and an assignment made earlier keeps working after the sector changes.
Switching sector is therefore reversible at any time.

If somebody reports that a qualification has disappeared from a dropdown while still being present
in the settings, this setting is the reason.

## Related skills

- `get_active_industries`, `update_active_industries`
- `list_qualifications`, `list_scheduling_rules`

## Trigger phrases

- "Why can I not pick that qualification on the shift any more?"
- "Was passiert, wenn ich die Branche umstelle?"
- "Kann ich mehrere Branchen gleichzeitig aktivieren?"
- "Werden Qualifikationen gelöscht, wenn ich eine Branche wähle?"
