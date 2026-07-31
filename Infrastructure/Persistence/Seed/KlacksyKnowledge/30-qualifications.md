---
name: explain_qualifications
description: |
  Explains who is allowed to work a shift: qualifications are a catalogue of skills and certificates
  that people hold at a proficiency level and that shifts can require at a minimum level. A mandatory
  requirement makes anyone who lacks it — missing, expired, or below the level — ineligible for that
  shift. Covers the catalogue entries themselves, assigning them to people, requiring them on shifts,
  and expiry. Use this when the user asks why someone cannot be assigned, how to record a certificate,
  or how to restrict a shift to trained staff.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - qualifikation
  - qualification
  - zertifikat
  - ausbildung
  - fähigkeit
  - darf nicht eingeteilt
  - voraussetzung
  - führerschein
synonyms:
  de: [qualifikation, zertifikat, ausbildung, fachkenntnis, sprachkenntnis, führerschein, voraussetzung, darf nicht eingeteilt werden, wer darf diesen dienst, abgelaufen]
  en: [qualification, certificate, skill, training, licence, requirement, not eligible, who may work this shift, expired]
  fr: [qualification, certificat, compétence, formation, permis, condition, non éligible, qui peut faire ce service]
  it: [qualifica, certificato, competenza, formazione, patente, requisito, non idoneo, chi può fare questo turno]
---

# Qualifications — who may work which shift

## Core idea (one sentence)

A qualification is recorded twice — as something a person **holds** and as something a shift
**requires** — and comparing the two decides who can be assigned.

## The catalogue

Every qualification exists first as a catalogue entry:

- **Name**, kept in several languages, plus an optional description.
- **Symbol** — an emoji for quick recognition in lists.
- **Type** — either *language* or *work*.
- **Time-limited** — marks qualifications that expire and must be renewed, such as certificates.
- **Country** — optional, for licences or diplomas that only apply in certain countries.
- **Category** — only for the *work* type: care at home, security, logistics, healthcare, hospitality,
  construction, cleaning, transport, or general. Language qualifications have no category.

The catalogue itself holds no information about any person. It is the vocabulary, not the assignment.

## What a person holds

Assigning a qualification to someone records a **proficiency level from 1 (basic) to 5 (expert)** and
optionally a period during which it is valid. That validity window is what makes an expiry date real:
a certificate that ran out last month no longer counts as held.

## What a shift requires

A shift can require a qualification at a **minimum level**, and mark it as mandatory or not.

This is where the two sides meet. A **mandatory** requirement makes anyone ineligible for that shift
who

- does not hold the qualification at all,
- holds it but it has expired, or
- holds it below the required level.

Ineligibility is not a warning that can be clicked away — it is applied when a replacement is looked
for and checked again before an assignment is committed. That is the whole point of the feature: the
plan cannot quietly put an untrained person on a shift that needs training.

A non-mandatory requirement expresses a preference rather than a barrier.

## Expiry

Qualifications with a validity window can be listed before they run out, together with expiring
contracts. Worth doing regularly — an expired certificate silently shrinks the pool of people
eligible for a shift.

## Related skills

- `list_qualifications` — what the catalogue contains
- `create_qualification` / `update_qualification` / `delete_qualification` — maintain the catalogue
- `set_client_qualification` — record what a person holds, with level and validity
- `set_shift_required_qualification` / `remove_shift_required_qualification` — what a shift demands
- `list_expiring_contracts` — also reports qualifications about to expire

## Trigger phrases

- "Why can't I assign this person to that shift?"
- "Record that she has the forklift licence."
- "Only staff with first aid should get this shift."
- "Which certificates expire soon?"
- "Wer darf diesen Dienst übernehmen?"
