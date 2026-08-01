---
name: explain_report_designer
description: |
  Explains building a printable document layout: picking the data it draws from, dragging fields
  into three heading zones, assembling one or more tables of columns with widths, sorting and cell
  frames, adding free text lines, an image and computed cells. Covers the notation a computed cell
  must use to produce a value at all, which names it can reference, and how a layout is made the
  one used for quick printing. Use this when the user asks how to design a printout, why a computed
  cell stays empty, or why the print entry does not appear.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  de:
    - report vorlage
    - druckvorlage
    - berichtsvorlage
    - formelfeld
    - spaltenbreite
  en:
    - report template
    - report designer
    - printout layout
    - formula field
synonyms:
  de: [report vorlage, druckvorlage, berichtsvorlage, report designer, formelfeld, formel im report, spaltenbreite, kopfzeile gestalten, standard druckvorlage, warum ist die formel leer]
  en: [report template, report designer, printout layout, formula field, column width, design a printout, default print template, why is my formula empty]
  fr: [modèle de rapport, concepteur de rapport, champ de formule, largeur de colonne, modèle par défaut]
  it: [modello di rapporto, designer di rapporti, campo formula, larghezza colonna, modello predefinito]
---

# Report templates — designing a printout

<!-- level:short -->

## Stage 1 — What this is for

A report template describes what a printed document looks like: what stands in the heading, which
columns the table has, what is summed at the bottom. Templates are managed at
`settings-reports`, in the **communication** section of the settings page.

Every template belongs to **one data source**, and that choice decides which names are available to
place. Seven sources exist: the work schedule, absences, the address list, a single address, groups,
shifts, and container templates.

A separate card, `settings-report-defaults`, decides which template is used for quick printing —
**one per data source**. Without an entry there, the print commands do not appear at all. That is
the usual reason for "the print entry is missing".

<!-- level:elements -->

## Stage 2 — The building blocks

Tabs of a template: general, data source, designer, preview, manual.

**Heading** — three zones per row, left, centre and right, filled by dragging names in. Rows can be
added and removed.

**Tables** — a template may hold **several**. Each has a title, a width as a percentage of the page,
its columns, and optionally a footer row of its own. A column carries a width, an alignment, a font,
a size between 6 and 48, bold/italic/underline, a text colour, a sort direction, and cell frames.

**Free text lines** can be placed before or after each table.

**An image** can be uploaded and placed in the heading; its size is adjustable.

**Merged columns** — several names can share one column, joined by a separator such as a line break,
a comma or a dash.

**Computed cells** can sit in a column or in a table footer.

**Column widths are proportions, not millimetres.** A width is weighed against the sum of all widths
in that table and then spread across the available page width. Elements flow from top to bottom in
their given order; nothing is positioned by coordinates.

**Cell frames** are set per side — top, right, bottom, left — choosing from none, thin, medium,
thick, dashed and double. The line thickness follows from that choice and is not entered separately;
the colour applies to all four sides at once.

Page size and orientation are **not** in the designer — they sit in the general tab.

<!-- level:effects -->

## Stage 3 — Computed cells

This is where most questions arise, because a wrong notation produces **nothing**, not an error.

**A computed cell must state its output explicitly**, in this form:

```
output 1, <expression>
```

The `1` is required. Writing only `output totalHours` puts the value in the wrong place and the cell
stays empty. `output = totalHours` is not valid either. Keywords are not case-sensitive, so `OUTPUT`
works just as well.

**Names are written plain**, without brackets or braces: `output 1, totalHours / totalRows`.

The names available to a computation are **not** the same list as the names you drag into columns.
For the work schedule the computations see totals such as the number of rows, total hours, total
surcharges, total working days and total expenses.

Available operations include the usual arithmetic plus worded operators such as `DIV`, `MOD`, `AND`,
`OR`, `NOT`, and functions including `IIF`, `ROUND`, `ABS`, `LEN`, `LEFT`, `RIGHT`, `MID`, `TRIM`,
`UCASE`, `LCASE`, `REPLACE`, `INSTR`, `SQR`, `LOG`, `EXP`, `TIMETOHOURS` and `TIMEOVERLAP`. Loops and
conditionals exist as well. The editor reports whether the expression is valid while it is typed; a
broken one yields an error marker in the output rather than a number.

## Related skills

- `list_report_templates`, `create_report_template`, `update_report_template`, `delete_report_template`
- `get_report_defaults`, `update_report_defaults`

## Trigger phrases

- "Wie baue ich eine eigene Druckvorlage?"
- "My formula field stays empty."
- "Warum erscheint der Drucken-Eintrag nicht im Menü?"
- "Kann ich zwei Tabellen in einen Report legen?"
- "How do I set the column width?"
