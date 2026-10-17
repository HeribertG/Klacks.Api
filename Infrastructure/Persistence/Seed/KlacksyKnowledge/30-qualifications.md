---
name: explain_qualifications
description: |
  Explains who is allowed to work a shift: qualifications are a catalogue of skills and certificates
  that people hold at a proficiency level and that shifts can require at a minimum level. A missing
  mandatory qualification always blocks the assignment. A level below the required minimum blocks only
  when the level is binding (set on the qualification, overridable per shift requirement); otherwise it
  is a warning and the planning wizard prefers sufficiently qualified people. Covers the catalogue, the
  levels people hold, requirements on shifts, binding versus hint-only levels, and expiry. Use this when
  the user asks why someone cannot be assigned, why a booking only warns, how to record a certificate,
  or how to restrict a shift to trained staff.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  - qualifikation
  - qualification
  - zertifikat
  - ausbildung
  - fähigkeit
  - darf nicht eingeteilt
  - voraussetzung
  - führerschein
  - mindeststufe
  - minimum level
synonyms:
  de: [qualifikation, zertifikat, ausbildung, fachkenntnis, sprachkenntnis, führerschein, voraussetzung, darf nicht eingeteilt werden, wer darf diesen dienst, abgelaufen, mindeststufe, stufe zu niedrig, mindeststufe verbindlich, nur hinweis]
  en: [qualification, certificate, skill, training, licence, requirement, not eligible, who may work this shift, expired, minimum level, level too low, binding level, hint only]
  fr: [qualification, certificat, compétence, formation, permis, condition, non éligible, qui peut faire ce service, niveau minimum, niveau insuffisant]
  it: [qualifica, certificato, competenza, formazione, patente, requisito, non idoneo, chi può fare questo turno, livello minimo, livello insufficiente]
---

# Qualifications — who may work which shift

## Core idea (one sentence)

A qualification is recorded twice — as something a person **holds** (at a level) and as something a
shift **requires** (at a minimum level) — and comparing the two decides who can be assigned.

<!-- level:short -->

## Stage 1 — What it is and what it is for

Qualifications keep untrained people off shifts that need training. A shift can require a
qualification and mark it as mandatory. If a person does not hold a mandatory qualification at all,
Klacks refuses the assignment. If the person holds it but **below the required minimum level**, it
depends on whether that level is **binding**: binding means refused, exactly like missing; not
binding (the default) means the assignment is allowed with a warning that names the held and the required level
(for example Basic instead of at least Proficient).

<!-- level:elements -->

## Stage 2 — How to set it up

### The catalogue

Every qualification exists first as a catalogue entry in the settings:

- **Name**, kept in several languages, plus an optional description.
- **Symbol** — an emoji for quick recognition in lists and in the schedule.
- **Type** — either *language* or *work*.
- **Time-limited** — marks qualifications that expire and must be renewed, such as certificates.
- **Country** — optional, for licences or diplomas that only apply in certain countries.
- **Category** — only for the *work* type: care at home, security, logistics, healthcare, hospitality,
  construction, cleaning, transport, or general. Language qualifications have no category.
- **Minimum level is binding** (de: "Mindeststufe ist verbindlich", en: "Minimum level is binding", fr: "Le
  niveau minimum est contraignant", it: "Il livello minimo è vincolante") — a checkbox, off by default. When ticked, a level below a shift's
  required minimum is treated like a missing qualification wherever the shift does not say otherwise. Qualifications that
  already existed when this checkbox was introduced were switched on by the update, so their earlier block stays.

The catalogue itself holds no information about any person. It is the vocabulary, not the assignment.

### What a person holds

Assigning a qualification to someone records a **proficiency level** — 1 Low, 2 Basic, 3 Proficient,
4 Advanced, 5 Expert — and optionally a period during which it is valid. That validity window is what
makes an expiry date real: a certificate that ran out last month no longer counts as valid.

### What a shift requires

In the shift form, each required qualification is one row with a **minimum level**, a **mandatory**
checkbox and a three-way choice for the level:

- *like the qualification* (de: "Wie Qualifikation", en: "As qualification", fr: "Comme la qualification", it:
  "Come la qualifica"; default) — the qualification's own checkbox decides; once a qualification is chosen in the
  row, the option also names what that currently means, e.g. "Wie Qualifikation: Verbindlich",
- *binding* (de: "Verbindlich", en: "Binding", fr: "Contraignant", it: "Vincolante") — a too-low level blocks on
  this shift, whatever the qualification says,
- *hint only* (de: "Nur Hinweis", en: "Hint only", fr: "Simple remarque", it: "Solo avviso") — a too-low level only
  warns on this shift, whatever the qualification says.

The effective rule for a row is therefore: the shift's choice, and if it is left at "like the
qualification", the checkbox on the qualification.

<!-- level:effects -->

## Stage 3 — What the comparison does

For a **mandatory** requirement:

| Situation | Manual booking | Error list | Planning wizard | Replacement search |
|---|---|---|---|---|
| Not held at all | refused | Error | never assigns | excluded |
| Below the level, level binding | refused | Error | never assigns new (an existing assignment counts as a gap, see below) | excluded |
| Below the level, not binding | allowed, with a warning | Warning | may assign, but prefers people at the required level | listed after the sufficiently qualified candidates, with the hint |

In the dialog **Cover absence (Recovery)** (de: "Ausfall decken (Recovery)", en: "Cover absence (Recovery)", fr:
"Couvrir une absence (récupération)", it: "Copri assenza (recupero)"), the list of other possible stand-ins marks such
a candidate with the badge (de: "Stufe zu niedrig", en: "Level too low", fr: "Niveau insuffisant", it: "Livello
troppo basso").

- The wizard's preference is a soft penalty that grows with the gap: level 1 on a level-3 shift weighs
  more than level 2. It ranks below reaching the employees' guaranteed hours, so it decides between
  otherwise equally good plans; on its own it never leaves a slot empty. Only the first stage of the
  planning wizard weighs this gap (see `explain_planning_wizards`).
- If a level is made binding later, an assignment the wizard finds already in the plan is not taken
  away; the wizard treats it like a gap below a non-binding level. The error list still shows it as Error.
- An expired mandatory qualification only warns, unless the company setting
  QUALIFICATION_EXPIRED_MANDATORY_BLOCKS is switched on — then it blocks like a missing one. It also
  blocks when the expired level is below a binding minimum level.
- A **non-mandatory** requirement never blocks; it expresses a preference.
- In a **scenario**, a manual booking is never refused for a qualification gap (the wizards still respect binding gaps there).
- A refusal is not a warning that can be clicked away, and it cannot be overridden — not by a
  supervisor, not by an administrator. The refused booking comes back as a conflict, and the message
  names the qualification and the shift. To place the person anyway, either record the qualification at a
  sufficient level, or make the level non-binding where that is acceptable.

### Expiry

Qualifications with a validity window can be listed before they run out, together with expiring
contracts. Worth doing regularly — an expired certificate silently shrinks the pool of people the
planner and the wizard consider fully qualified.

### Related skills

- `list_qualifications` — what the catalogue contains
- `create_qualification` / `update_qualification` (parameter `isLevelBinding`: true or false) /
  `delete_qualification` — maintain the catalogue
- `set_client_qualification` — record what a person holds, with level and validity
- `set_shift_required_qualification` (parameter `levelBindingOverride`: inherit, binding or hint) /
  `remove_shift_required_qualification` — what a shift demands
- `list_expiring_contracts` — also reports qualifications about to expire

### Trigger phrases

- "Why can't I assign this person to that shift?"
- "Why does the booking only warn although her level is too low?"
- "Record that she has the forklift licence."
- "Only staff with first aid should get this shift."
- "Which certificates expire soon?"
- "Wer darf diesen Dienst übernehmen?"

<!-- level:pitfalls -->

## Stage 4 — Consequences, edge cases, examples

### Example: binding versus hint only

- **Pflegefachperson** (care nurse), minimum level binding: a shift requires the qualification at
  level 3. A nurse at level 2 can never be planned on it — manual booking is refused, the wizard skips
  her, the replacement search leaves her out. The same nurse at level 3 or higher is fine.
- **Kasse** (cash desk), hint only: a shift requires level 3. An employee at level 2 can be booked; the
  booking and the error list show a warning naming level 2 (Basic) instead of at least level 3 (Proficient), and the wizard prefers level-3
  people when that costs no planned hours.

### Existing data: too-low levels now only warn

When this rule was introduced, every qualification started with the binding checkbox off and every
shift row at "like the qualification". So a too-low level is a warning everywhere until someone
decides otherwise — before, a manual booking below the level was refused. Tick the checkbox on the
qualifications that must stay strict (typically regulated care or safety certificates), or set the
shift row to "binding".

### The shift row wins

A shift row set to "hint only" stays lenient even when the qualification is binding, and a row set to
"binding" blocks even when the qualification is not. When a booking behaves unexpectedly, check the
row on the shift first, then the checkbox on the qualification.

### Missing is always strict

The binding choice only concerns the level. A mandatory qualification the person does not hold at all
blocks regardless of any checkbox.
