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

Ein Container bündelt mehrere Aufgaben-Dienste und Absenzen zu einem Tagespaket; du planst eine Person für den Container, und die enthaltenen Aufgaben hängen automatisch daran.

<!-- level:short -->

## Stufe 1 — Wofür ist das?

Ein Container ist ein Dienst, der selbst keine Arbeit beschreibt, sondern mehrere andere Dienste zu einer Tour oder einem Tagespaket zusammenfasst. Beispiel: Eine Kontrolltour besteht aus Tankstelle, Lager und drei Kundenbesuchen. Statt jede Aufgabe einzeln zu planen, legst du die Zusammensetzung einmal für jeden Wochentag in einer Vorlage fest und planst dann nur noch eine Person für den ganzen Container. Im Einsatzplan siehst du dafür einen einzigen Eintrag, die enthaltenen Aufgaben laufen im Hintergrund mit. Für einen einzelnen Tag kannst du die Zusammensetzung abweichend festlegen, ohne die Vorlage zu verändern.

<!-- level:elements -->

## Stufe 2 — So gehst du vor

**Container anlegen** (Dienste-Seite, Navi-Icon "Alle Dienste"):

1. Auf **+ neue Bestellung** (de: "+ neue Bestellung", en: "+ New Order") klicken; der Button erscheint nur mit dem Recht, Dienste anzulegen (Administratoren und Supervisoren).
2. In der Maske den Schalter **Experten Modus** (de: "Experten Modus", en: "Expert Mode") einschalten und das Häkchen **Ist ein Container** (de: "Ist ein Container", en: "Is a container") setzen. Das Häkchen gibt es nur, solange die Bestellung noch nicht versiegelt ist. Es leert Kunde und Adresse und schaltet die Zeitfenster-Option aus; ein Container hat nie einen Kunden.
3. Gruppe, Wochentage und Gültigkeit wie bei jedem Dienst festlegen, dann versiegeln (Schloss-Button in der Karte "Allgemeines"). Danach erscheint der Container in der Sicht **Container** (de: "Container", en: "Container").

**Vorlage pflegen:** In der Sicht "Container" öffnet das Box-Icon in der Zeile die **Schichtvorlage** (de: "Schichtvorlage", en: "Shift Template"). Dort:

- den Wochentag wählen (Radio-Buttons; nur Wochentage, die am Container aktiv sind),
- Aufgaben aus der Tabelle der verfügbaren Aufgaben per Drag & Drop in die Karte **Zugewiesene Aufgaben** (de: "Zugewiesene Aufgaben", en: "Assigned Tasks") ziehen,
- Absenz-Arten aus dem Tab **Beschäftigung** (de: "Beschäftigung", en: "Employment") ziehen,
- die Lage der Einträge im Zeitlineal links anpassen; Rechtsklick auf einen Eintrag öffnet **Eigenschaften...** (Briefing, Debriefing, Anreise, Rückreise).

Hält schon jemand anderes dieselbe Vorlage offen, erscheint ein Hinweis und die Seite ist schreibgeschützt. Die Karten der Vorlagenseite sind auf der Dienste-Seite (`explain_page_shifts`) beschrieben.

**Container einplanen:** Im Einsatzplan wie jeden Dienst in die Zelle einer Person ziehen. Sichtbar ist nur dieser eine Eintrag; die enthaltenen Aufgaben sind unsichtbare Unter-Einsätze. Doppelklick oder Kontextmenü **Öffnen...** (de: "Öffnen...") zeigt den Inhalt.

**Einen Tag abweichend festlegen:** Im Schicht-Bereich des Einsatzplans (die Dienste-Zeilen) Rechtsklick auf die Zelle des Containers am gewünschten Tag und **Dienst editieren** (de: "Dienst editieren", en: "Edit shift"). Tage mit einer solchen Abweichung tragen oben links ein Bleistift-Icon. **Dienst zurücksetzen** (de: "Dienst zurücksetzen", en: "Reset shift") im selben Kontextmenü löscht die Abweichung, der Tag fällt auf die Vorlage zurück; der Eintrag erscheint nur bei Tagen mit Abweichung und nur, solange dort noch niemand eingeplant ist.

Aufgaben-Dienste, die in der Vorlage eines Containers stehen, tragen im Schicht-Bereich ein Container-Icon unten links.

<!-- level:effects -->

## Stufe 3 — Zusammenspiel und Datenfluss

**Drei Daten-Schichten**

```
Container-Shift  (ShiftType=IsContainer)         ← die Hülle, keine eigene Tätigkeit
   └── ContainerTemplate  (pro Wochentag + IsHoliday)   ← Bauplan
         └── ContainerTemplateItems  (ShiftId XOR AbsenceId)   ← Bausteine
                                       + StartItem/EndItem
                                       + Briefing/Debriefing/Travel
```

**Override-Schicht (optional):** Eine Abweichung für einen Tag (`ContainerShiftOverride`) ist ein **vollständiger Stand** der Tagesplanung für `(ContainerId, Date)`, kein Delta. Sie hat Vorrang vor der Vorlage: existiert sie, wird die Vorlage für diesen Tag gar nicht mehr gelesen.

**Expansion beim Buchen** (`ContainerWorkExpansionService.ExpandAsync(containerWork, date)`, aufgerufen beim Einzelbuchen und beim Sammel-Einfügen):

1. Prüfen, dass der Dienst ein Container ist, sonst Abbruch.
2. Abweichung für `(containerId, date)` suchen.
3. Wenn vorhanden → Items der Abweichung, sonst → Vorlage des Wochentags (bevorzugt die Vorlage ohne Feiertags-Kennzeichen).
4. Pro Item mit `ShiftId` → neuer Unter-Einsatz mit `ParentWorkId = container.Id`.
5. Pro Item mit `AbsenceId` → neue Pause (Absenz) mit `ParentWorkId = container.Id`.
6. Einträge für Briefing, Debriefing und Anreise/Rückreise ergänzen.
7. Bei Zeitfenster-Diensten im Item wird die im Item gewählte Start-/Endzeit verwendet, sonst die Item-Zeiten.

**Weitergabe auf den Eltern-Einsatz** (`ContainerWorkCascadeService`, eine Anwendungs-Kaskade, keine Datenbank-Kaskade):

- `DeleteChildrenAsync` — löscht alle Unter-Einsätze mit.
- `MoveChildrenAsync(newDate, newClientId)` — Datum und Mitarbeitende werden für alle Unter-Einsätze mitgezogen.
- `UpdateLockLevelAsync(level)` — die Sperrstufe des Eltern-Einsatzes wird auf alle Unter-Einsätze übertragen.
- `RestoreChildrenAsync` — holt nur die Unter-Einsätze zurück, die beim selben Löschen vom selben Benutzer mit-gelöscht wurden.

**Anzeige:** `get_schedule_entries` filtert `parent_work_id IS NULL`; im Einsatzplan erscheint daher nur der Eltern-Einsatz. Dieselbe Einschränkung gilt für die Belegungszahlen im Schicht-Bereich (`get_shift_schedule`): Unter-Einsätze zählen dort nicht als Belegung ihres Aufgaben-Dienstes.

**Dashboard:** Ein Container zählt als 1 Dienst im Dienste-Balken des Ressourcen-Monitors; Aufgaben-Dienste, die in der Vorlage eines Containers stehen, werden dort nicht zusätzlich gezählt.

**Drei ähnlich klingende Begriffe**

| Begriff | Bedeutung |
|---|---|
| `ShiftType == IsContainer (1)` | Dieser Dienst IST ein Container. |
| `isInTemplateContainer == true` (Oberflächen-Kennzeichen) | Dieser Aufgaben-Dienst ist als Baustein in der Vorlage eines Containers enthalten. |
| `ContainerLock` | Gemeinsame Bearbeitungssperre: nur eine Sitzung bearbeitet dieselbe Vorlage bzw. denselben Tag; eine Sperre ohne Lebenszeichen wird beim nächsten Versuch automatisch freigegeben. |

## Verwandte Skills

- `explain_shift_lifecycle_order_to_shift` — ein Container durchläuft denselben Versiegelungs-Ablauf
- `explain_shift_sporadic` — Container können sporadisch sein
- `explain_shift_time_range` — Zeitfenster-Dienste in Containern
- `explain_page_shifts` — Vorlagenseite und Erfassungsmaske

## Trigger-Phrasen

- "Was ist ein Container-Dienst?"
- "Warum tauchen meine Sub-Shifts nicht im Einsatzplan auf?"
- "Wie passe ich einen Container nur für einen Tag an?"
- "Was bedeutet 'Dienst editieren' im Container?"
- "How does container expansion work in Klacks?"
- "Pourquoi mes sous-tâches n'apparaissent-elles pas dans le planning ?"
- "Cosa significa 'modifica del giorno' per un contenitore?"

<!-- level:pitfalls -->

## Stufe 4 — Folgen, Grenzfälle, Endgültiges

- **Die Unter-Einsätze entstehen einmal beim Buchen.** Danach ändert eine Änderung der Vorlage oder einer Tagesabweichung bereits gebuchte Container nicht mehr; nur neu gebuchte Tage folgen der neuen Zusammensetzung.
- **Abweichung ändern oder zurücksetzen geht nur ohne Einsatz.** Sobald für den Container an diesem Datum ein Einsatz existiert, lehnt der Server das Ändern und das Zurücksetzen der Abweichung mit einem Konflikt ab; im Kontextmenü fehlt dann auch "Dienst zurücksetzen". Wird der Einsatz gelöscht, ist die Abweichung wieder bearbeitbar.
- **Die Abweichung ist ein ganzer Stand, kein Delta.** Spätere Änderungen an der Vorlage erreichen Tage mit Abweichung nicht; ändere dort die Abweichung selbst oder setze sie zurück.
- **Löschen, Verschieben und Bestätigen des Containers betreffen alles darunter.** Löschen (einzeln und gesammelt), Verschieben oder einer anderen Person zuweisen und Bestätigen am Eltern-Einsatz laufen über alle Unter-Einsätze; dabei wird auch die Sperrstufe übertragen. Wiederherstellen bringt nur zurück, was beim selben Löschen vom selben Benutzer mit-gelöscht wurde — früher oder von anderen entfernte Unter-Einsätze bleiben gelöscht.
- **Unter-Einsätze zählen nicht als Belegung.** Ein Aufgaben-Dienst, der nur über einen Container gebucht wird, zeigt im Schicht-Bereich keine Belegung durch diese Buchungen. Das gilt auch für sporadische Aufgaben-Dienste: Ihre Grenzen werden nur von eigenständigen Einsätzen verbraucht.
- **Nach dem Versiegeln ist die Container-Eigenschaft fest.** Dienst-Typ (Container oder Aufgabe) gehört zu den gesperrten Feldern; ein falsch angelegter Container lässt sich nur durch Löschen der versiegelten Bestellung (Administrator, über Klacksy oder die API, solange nichts darin verplant ist) und Neuanlegen ersetzen.
- **Zwei Sitzungen, eine Vorlage.** Wer die Vorlage oder die Tagesabweichung schon geöffnet hat, sperrt sie für andere (auch für ein zweites eigenes Browser-Fenster); die Seite öffnet dann schreibgeschützt mit Hinweis auf den Benutzer.
