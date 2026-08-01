---
name: explain_export_formats_model
description: |
  Explains handing sealed data over to an outside system: which target formats can be switched on
  for bookkeeping, for payroll and for hours per person, that three plain formats always stay
  available, and what a disabled format does on a manual download versus an automatic handover.
  Also covers the correction a support team can paste in to adjust separator, encoding, date shape
  or wage keys without waiting for a release, and where such a correction does and does not take
  effect. Use this when the user asks which target systems are supported or why a handover produced
  nothing.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  mul:
    - datev
  de:
    - exportformat
    - export format
    - lohnexport
    - buchungsstapel
    - trennzeichen
  en:
    - export format
    - payroll export
    - accounting handover
synonyms:
  de: [exportformat, export-formate, lohnexport, buchungsstapel, fibu-export, trennzeichen ändern, zeichensatz, export korrektur, welches format für unser lohnprogramm]
  en: [export format, payroll export, accounting export, change delimiter, encoding, export correction, which format for our payroll system]
  fr: [format d export, export de paie, export comptable, changer le séparateur, encodage, correction d export]
  it: [formato di esportazione, esportazione paghe, esportazione contabile, cambiare separatore, codifica, correzione esportazione]
---

# Export formats — handing data to an outside system

<!-- level:short -->

## Stage 1 — What this is for

Once a period is sealed, its data can be written in the shape another system expects. There are
three purposes, and a format always belongs to exactly one:

- **Orders** (de: "Bestellungen", en: "Orders") — bookkeeping and financial systems.
- **Payroll** (de: "Lohn & Gehalt", en: "Payroll") — wage keys and absence codes per person and day.
- **Employee hours** (de: "Mitarbeiterstunden", en: "Employee hours") — hours per person for a span.

**Three formats are always available and cannot be switched off:** comma-separated, JSON and XML,
all on the bookkeeping side. Everything else is optional and country- or vendor-specific.

**Careful with the default:** when nothing has ever been chosen, **every** optional format counts
as switched on — not none.

<!-- level:elements -->

## Stage 2 — The two cards

### Choosing formats

Card anchor: `settings-export-formats` (assistant target `export-formats`). It sits in the
**general** section of the settings page.

Plain checkboxes, saved the moment they are clicked; there is no save button. The card lists the
bookkeeping and payroll formats. The three employee-hours formats are **not** shown here — they are
always available.

One vendor appears with two variants under a shared parent, because it offers both a bookkeeping
and a payroll format. Every other format is a flat single entry.

### Corrections

Card anchor: `settings-export-format-overrides` (assistant target `export-format-overrides`).

When a target system rejects a file, support can supply a small correction that takes effect
immediately, without waiting for a release. It is a plain object of key and value — **not** a patch
language with operations — and only nine settings can be adjusted:

- for bookkeeping and employee hours: date shape, time shape, currency code, language
- for payroll: separator, character encoding, the base wage key, the surcharge wage key, and the
  mapping from absence kinds to codes

Anything beyond that — the column count, the record layout, the nesting of an XML file — cannot be
corrected this way.

Fields on the card: the target format, the correction itself, a note for the support ticket, and a
switch for whether it is active. **Download the preview first** — it produces a test file with the
correction applied. A faulty correction is reported plainly in the preview, whereas during a real
export it is skipped and the export runs with the defaults.

<!-- level:effects -->

## Stage 3 — What actually happens

**A switched-off format behaves differently depending on the path:**

- A manual download of a bookkeeping or payroll file is **refused with an error**.
- The **automatic** payroll handover at period closing is **skipped silently** and only noted in the
  log. The sealing itself goes through unaffected. If a payroll file is missing after closing a
  period, this is the first thing to check.
- Employee-hours exports are not gated at all.

**Where a correction takes effect** — bookkeeping exports, range exports, employee-hours exports,
and the automatic payroll handover at period closing. It does **not** take effect on a manual
payroll download; that one always uses the defaults. A correction that seems to be ignored is
almost always this case.

**The version note is a hint, not a barrier.** A correction records the version it was saved under,
and the card warns when the application has moved on. Nothing is blocked — an old correction keeps
being applied. Note also that re-saving stamps the current version, so opening and saving a
correction clears the warning without changing anything.

## Related skills

- `get_export_formats`, `update_export_formats`
- `list_export_format_overrides`, `save_export_format_override`, `delete_export_format_override`, `preview_export_format_override`

## Trigger phrases

- "Welches Format brauche ich für unsere Lohnbuchhaltung?"
- "The accounting system rejects our file — can we change the separator?"
- "Nach dem Periodenabschluss fehlt die Lohndatei."
- "Warum wirkt meine Korrektur beim Download nicht?"
- "Can we export as plain CSV?"
