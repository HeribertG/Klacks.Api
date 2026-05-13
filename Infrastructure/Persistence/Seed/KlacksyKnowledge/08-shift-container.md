---
name: explain_shift_container
description: |
  Explains the Container Shift in Klacks (ShiftType = IsContainer). A container
  bundles multiple task-shifts and absences via a weekday-keyed ContainerTemplate.
  When booked, ContainerWorkExpansionService creates one visible parent Work and
  invisible sub-Works / sub-Breaks linked via ParentWorkId. Optional day-level
  ContainerShiftOverride replaces the template snapshot-style for a single date.
  Use this when the user asks what a container shift is, why sub-works are not
  shown in the schedule grid, or how "Dienst editieren" / "Dienst zurücksetzen"
  works.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - container
  - containerwork
  - template
  - override
  - sub-work
  - parentwork
  - expansion
  - dienst editieren
synonyms:
  de: [container, containerschicht, vorlage, bauplan, tagesüberschreibung, sub-buchung, eltern-buchung]
  en: [container, container shift, template, override, sub-work, parent work, expansion]
  fr: [conteneur, modèle, surcharge, sous-travail, travail parent, expansion]
  it: [contenitore, modello, sovrascrittura, sotto-lavoro, lavoro padre, espansione]
---

# Container Shift — Hülle für mehrere Teil-Shifts

## Kern-Idee (1 Satz)

Ein **Container** (`ShiftType = IsContainer`) bündelt mehrere Task-Shifts und
Absenzen via einen Wochentags-Bauplan (`ContainerTemplate`) und expandiert bei
jeder Buchung zu **einem sichtbaren Eltern-Work** plus **unsichtbaren Sub-Works /
Sub-Breaks** — optional vorher überschrieben durch ein tagesgenaues
`ContainerShiftOverride`.

## Drei Daten-Schichten

```
Container-Shift  (ShiftType=IsContainer)         ← die Hülle, keine eigene Tätigkeit
   └── ContainerTemplate  (pro Wochentag + IsHoliday)   ← Bauplan
         └── ContainerTemplateItems  (ShiftId XOR AbsenceId)   ← Bausteine
                                       + StartItem/EndItem
                                       + Briefing/Debriefing/Travel
```

## Override-Schicht (optional)

`ContainerShiftOverride` ist ein **vollständiger Snapshot** (kein Delta!) der
Tagesplanung für `(ContainerId, Date)`. Override hat **Vorrang** vor Template.
Lebenszyklus:

```
editierbar  →  gesperrt (sobald Work existiert)  →  wieder editierbar
                                                     (nach Work-Löschung)
```

UI-Aktionen:
- "Dienst editieren" → Override anlegen / bearbeiten (Pencil-Icon TopLeft).
- "Dienst zurücksetzen" → Override löschen → Tag fällt auf Template zurück.

## Expansion beim Buchen

`ContainerWorkExpansionService.ExpandAsync(containerWork, date)`:

1. `ShiftType == IsContainer` prüfen, sonst Abbruch.
2. Override für `(containerId, date)` suchen.
3. Wenn vorhanden → Override-Items, sonst → passendes ContainerTemplate.
4. Pro Item mit `ShiftId` → neues `Work` mit `ParentWorkId = container.Id`.
5. Pro Item mit `AbsenceId` → neuer `Break` mit `ParentWorkId = container.Id`.
6. WorkChanges für Briefing/Debriefing/Travel ergänzen.
7. Alles in einer Transaktion speichern.

## Cascading auf den Eltern-Work

`ContainerWorkCascadeService` (Applikations-Cascade, **kein** DB-Cascade —
Multi-User-Deadlock-Schutz):

- `DeleteChildrenAsync` — alle Children löschen.
- `MoveChildrenAsync(newDate)` — Date-Delta auf alle Children.
- `UpdateLockLevelAsync(level)` — LockLevel auf alle Children.

## Anzeige im Schedule-Grid

`get_schedule_entries` filtert `WHERE parent_work_id IS NULL` → nur der
**Eltern-Container-Work** erscheint. Die Sub-Works zählen aber zu den
Engagement-Counts ihrer jeweiligen Task-Shifts in der Shift-Section
(separate Query, ohne den Filter).

## Drei ähnlich klingende Begriffe — Abgrenzung

| Begriff | Bedeutung |
|---|---|
| `ShiftType == IsContainer (1)` | Dieser Shift **IST** ein Container. |
| `isInTemplateContainer == true` (FE-Flag) | Dieser Task-Shift ist als Sub-Item in einem Template enthalten. |
| `ContainerLock` | Optimistic-Lock gegen Multi-User-Konflikt. |

## Verwandte Skills

- `explain_shift_lifecycle_order_to_shift` — ein Container durchläuft denselben Sealing-Workflow
- `explain_shift_sporadic` — Container können `IsSporadic` sein
- `explain_planning_assistant` — der Assistent expandiert Container automatisch

## Trigger-Phrasen

- "Was ist ein Container-Dienst?"
- "Warum tauchen meine Sub-Shifts nicht im Einsatzplan auf?"
- "Wie passe ich einen Container nur für einen Tag an?"
- "Was bedeutet 'Dienst editieren' im Container?"
- "How does container expansion work in Klacks?"
- "Pourquoi mes sous-tâches n'apparaissent-elles pas dans le planning ?"
- "Cosa significa 'modifica del giorno' per un contenitore?"
