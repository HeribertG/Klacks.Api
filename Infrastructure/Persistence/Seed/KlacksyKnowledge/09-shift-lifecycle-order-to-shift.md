---
name: explain_shift_lifecycle_order_to_shift
description: |
  Explains the four-stage lifecycle of a shift in Klacks — from the editable
  order via the immutable sealed order to the active plannable shift and its
  cut pieces — and walks through every card of the order entry mask (General
  with expert mode and the sealing lock, Group, Required Qualifications,
  Hours and Weekdays, Macro, Address, Special Features, Default Expenses).
  Use this when the user asks what an order is, how to create one, why an
  order is not yet bookable, or what the fields of the order mask do.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - lifecycle
  - lebenszyklus
  - originalorder
  - sealedorder
  - originalshift
  - splitshift
  - bestellung
  - sealing
  - sealen
  - workflow
synonyms:
  de: [bestellung, auftrag, skizze, vertrag, sealen, versiegeln, schicht-schnitt, lebenszyklus]
  en: [order, draft, contract, sealed, lifecycle, shift cut, status]
  fr: [commande, ébauche, contrat, verrouillé, cycle de vie, coupe]
  it: [ordine, bozza, contratto, sigillato, ciclo di vita, taglio]
---

# Workflow Bestellung → Shift — die 4 Status-Stufen

## Kern-Idee (1 Satz)

Ein Dienst durchläuft vier Stufen — **Bestellung** (frei bearbeitbar),
**versiegelte Bestellung** (unveränderlicher Auftrags-Snapshot), **planbare
Schicht** (aktive Planung) und **Teilstücke** (Tages-/Zeitschnitt) — und
**genau eine** Transition, das Versiegeln, erzeugt **einmalig** die planbare
Schicht. Eine Bestellung ist ein echter Auftrag, keine Skizze — sie ist nur
noch nicht versiegelt.

## Die vier Stufen

(Die internen Statuswerte in Backticks sind NUR interne Anker — gegenüber
Usern immer die fettgedruckten Begriffe verwenden.)

| Wert | Intern | Bedeutung |
|---|---|---|
| 0 | `OriginalOrder` | **Bestellung** (Auftrag). Frei editierbar. Nicht im Einsatzplan. Nicht buchbar. |
| 1 | `SealedOrder` | **Versiegelte Bestellung** (freigegebener Auftrag). Permanent unveränderlich. Auftrags-Snapshot. |
| 2 | `OriginalShift` | **Planbare Schicht**. Beim Versiegeln automatisch erzeugt, mit Verweis auf die versiegelte Bestellung. Editierbar, schneidbar, buchbar. |
| 3 | `SplitShift` | **Teilstück** (Tages-/Zeitschnitt) der planbaren Schicht. |

**Einzige Ausnahme von der Unveränderlichkeit — nachträgliches Bis-Datum:** Hat eine
versiegelte Bestellung KEIN Bis-Datum (z. B. weil sich der Auftrag regelmässig verlängert
oder sein Ende noch nicht definiert ist), kann das Bis-Datum auch nach dem Versiegeln noch
gesetzt werden — als einzige zulässige Änderung. Voraussetzung: Ab diesem Datum dürfen noch
keine Dienste verplant sein. Sobald ein Bis-Datum gesetzt ist, ist auch dieses Feld
gesperrt wie alle anderen.

## Die einmalige Sealing-Transition

```
┌──────────────────┐      Versiegeln          ┌──────────────────┐
│   Bestellung     │ ───────────────────────► │   versiegelte    │
│  (Status = 0)    │   (einmaliger Trigger,   │   Bestellung     │
│  bearbeitbar     │    erzeugt simultan      │  (Status = 1)    │
└──────────────────┘    die planbare          │  unveränderlich  │
                        Schicht)              └──────────────────┘
                                                        │
                                                        │ Erzeugung nur HIER
                                                        ▼
┌──────────────────┐      Zuschnitt           ┌──────────────────┐
│   Teilstück      │ ◄─────────────────────── │ planbare Schicht │
│  (Status = 3)    │    (2-Phasen-Cut)        │  (Status = 2)    │
└──────────────────┘                          └──────────────────┘
```

`ShiftRepository.PutWithSealedOrderHandling` cloned **nur**, wenn:

```
updatedShift.Status == SealedOrder  AND  previousStatus != SealedOrder
```

Bei jedem späteren PUT auf einem bereits gesealten Shift → kein erneuter Klon.

## Wo Mitarbeiter gebucht werden

`Work`-Entitäten hängen **ausschließlich** an Shifts mit Status ≥ 2 — also
`OriginalShift` oder `SplitShift`. Versuche, auf `OriginalOrder` oder
`SealedOrder` zu buchen, sind technisch und semantisch ausgeschlossen.

## Beispiel-Lebenszyklus

```
2026-05-01  Anwender: neuer Shift "Hochzeit Müller" (OriginalOrder).
            Iterative Verfeinerung der Details.

2026-05-03  Kunde bestätigt → Status=SealedOrder + Save.
            Backend: SealedOrder festgeschrieben (id=A) + Klon OriginalShift
            (id=B, OriginalId=A) erzeugt.

2026-05-04  Disposition öffnet id=B, trägt 2 Mitarbeiter ein.

2026-05-05  Anwender zerschneidet id=B in 2 SplitShifts (id=C, id=D),
            ParentId=B, RootId=B.

2026-06-01  Periode geschlossen → LockLevel=Closed auf den Works auf C+D.
            id=A (SealedOrder) ist nie verändert worden — bleibt als Vertrag.
```

## Orthogonales Locking

Einzelne `Work`/`Break`-Einträge tragen ein separates `LockLevel` (None /
Confirmed / Approved / Closed). Das ist **NICHT** zu verwechseln mit
`ShiftStatus.SealedOrder`. Sealing eines Works ist Entry-Level, Sealing
eines Shifts ist Lifecycle-Level.

## Die Erfassungsmaske einer Bestellung (`/workplace/new-shift`, bearbeiten via `/workplace/edit-shift/:id`)

Neue Dienste entstehen immer als Bestellung — der Button **+ Dienst erfassen** auf der
Dienste-Seite öffnet diese Maske. Die Kopfleisten-Suche ist hier ausgeblendet;
Speichern/Verwerfen läuft über die Fusszeile des Arbeitsbereichs. Jede Card hat oben
rechts einen Auf-/Zuklapp-Pfeil. Cards in dieser Reihenfolge:

1. **Allgemeines** (de: "Allgemeines", en: "General", fr: "Général", it: "Generale") —
   Schalter **Experten Modus** (de: "Experten Modus", en: "Expert Mode", fr: "Mode expert")
   blendet die Experten-Cards ein (die Wahl wird im Browser gemerkt; beim Ausschalten wird
   die Zeitrahmen-Option am Dienst zurückgesetzt). Felder: **Abkürzung*** (Input
   `abbreviation`, `data-klacksy-target="shift-form.abbreviation"`, max. 6 Zeichen — wird
   beim Tippen des Namens automatisch vorgeschlagen, solange das Feld unberührt ist),
   **Name*** (Input `name`, `data-klacksy-target="shift-form.name"`), **Von Datum*** /
   **Bis Datum** (Datepicker) und **Notizen** (Rich-Text-Editor). Rechts der **Lock-Button**
   `shift-lock-btn` (grün, offenes Schloss) — nur im Status Bestellung
   sichtbar; Tooltip: "Nach Sperrung ist der Auftrag unveränderlich und steht zur Planung
   bereit." Er wird erst aktiv, wenn Abkürzung, Name, Von-Datum, mindestens ein Wochentag,
   mindestens eine Gruppe, Anzahl Aufgaben > 0 und Anzahl Mitarbeiter > 0 gültig sind.
   Klick setzt den Status auf versiegelt — nach dem Speichern ist das nicht
   umkehrbar. In jedem anderen Status erscheint statt des Buttons ein geschlossenes Schloss
   und alle Felder sind deaktiviert — einzige Ausnahme: das Feld **Bis Datum** bleibt auf
   versiegelten Bestellungen editierbar, solange noch KEIN Bis-Datum gesetzt ist (offenes
   Auftragsende, z. B. bei sich verlängernden Aufträgen); es darf dann nur so gesetzt
   werden, dass ab diesem Datum keine Dienste verplant sind, und ist danach ebenfalls
   gesperrt. Checkbox **Ist ein Container** (de: "Ist ein
   Container", en: "Is a container", fr: "Est un conteneur", it: "È un contenitore") — nur
   im Experten-Modus und nur im Status Bestellung sichtbar; aktivieren leert Kunde/Adresse
   und schaltet die Zeitrahmen-Option aus.
2. **Gruppe** (de: "Gruppe", en: "Group", fr: "Groupe", it: "Gruppo") — Zuordnung des
   Dienstes zu Gruppen; Card erscheint nur, wenn Gruppen existieren. Eine Info-Box mahnt
   **Bitte wählen Sie mindestens eine Gruppe aus** (de: "Bitte wählen Sie mindestens eine
   Gruppe aus", en: "Please select at least one group", fr: "Veuillez sélectionner au
   moins un groupe", it: "Si prega di selezionare almeno un gruppo"), solange keine Gruppe
   gewählt ist. Darunter ein Gruppen-Auswahl-Dropdown und die Tabelle der zugeordneten
   Gruppen mit den Spalten **Gruppe** und **Beschreibung** (de: "Beschreibung", en:
   "Description", fr: "Description", it: "Descrizione") plus Papierkorb zum Entfernen.
3. **Erforderliche Qualifikationen** (de: "Erforderliche Qualifikationen", en: "Required
   Qualifications", fr: "Qualifications requises", it: "Qualifiche richieste") — Filter
   nach Typ (de: "Alle Typen", en: "All types") und Land (de: "Alle Länder", en: "All
   countries") sowie Button **Qualifikation hinzufügen** (de: "Qualifikation hinzufügen",
   en: "Add qualification", fr: "Ajouter une qualification", it: "Aggiungi qualifica").
   Tabelle mit **QUALIFIKATION** (en: "QUALIFICATION"), **MINDESTSTUFE** (en: "MIN LEVEL",
   Stufen 1–5: Gering / Grundlegend / Kompetent / Fortgeschritten / Experte) und
   **PFLICHT** (en: "MANDATORY", Checkbox). Sind noch keine Qualifikationen definiert,
   erscheint der Hinweis, sie zuerst in den Einstellungen anzulegen.
4. **Stunden und Wochentage** (de: "Stunden und Wochentage", en: "Hours and Weekdays", fr:
   "Heures et jours de la semaine", it: "Ore e Giorni Feriali") — **Von Zeit hh:mm**, **Bis
   Zeit hh:mm**, **Dauer** (en: "Duration"); im Experten-Modus (nicht bei Containern)
   zusätzlich die Zeitrahmen-Checkbox "Dienst innerhalb des Zeitrahmens (Von Zeit hh:mm -
   Bis Zeit hh:mm). Bitte in Feld Dauer die Dauer des Diensten angeben" (= TimeRange:
   der Dienst liegt flexibel in diesem Zeitfenster, die Dauer zählt). Darunter die
   Wochentags-Checkboxen Montag–Sonntag plus **Feiertag, egal an welchen Wochentag** und
   **Feiertag an selektiertem Wochentag**.
5. **Macro** (de: "Macro", en: "Macro", fr: "Macro", it: "Macro") — nur im Experten-Modus
   sichtbar: Dropdown **Macro Liste** (de: "Macro Liste", en: "Macro list", fr: "Liste des
   macros", it: "Elenco macro") zur Zuordnung eines Macros für die Dauer-/Lohnberechnung;
   darunter ein schreibgeschütztes Textfeld mit der Beschreibung des gewählten Macros.
6. **Adresse** (de: "Adresse", en: "Address", fr: "Adresse", it: "Indirizzo") — nicht bei
   Containern: Suchfeld **Adresse suchen** (de: "Adresse suchen", en: "Search address",
   fr: "Rechercher une adresse", it: "Cerca indirizzo") mit Platzhalter **Name oder ID
   Nummer eingeben** (de: "Name oder ID Nummer eingeben", en: "Enter name or ID number")
   und Vorschlagsliste, Button **Auswählen** (de: "Auswählen", en: "Select", fr:
   "Sélectionner", it: "Seleziona") sowie die Anzeige **Ausgewählte Adresse** (de:
   "Ausgewählte Adresse", en: "Selected address") mit Papierkorb zum Entfernen.
7. **Spezielle Merkmale** (de: "Spezielle Merkmale", en: "Special Features", fr:
   "Caractéristiques spéciales", it: "Caratteristiche Speciali") — nur im Experten-Modus
   oder bei Containern: Checkbox **sporadischer Einsatz** (de: "sporadischer Einsatz", en:
   "Sporadic use", fr: "Utilisation sporadique", it: "Utilizzo sporadico") mit
   Periodizitäts-Dropdown (wöchentlich, monatlich, Quartal, Semester, jährlich, Laufzeit
   des Vertrages); **Briefing**/**Debriefing** und **Anreisezeit**/**Rückreisezeit** (en:
   "Travel time before"/"Travel time after"); **Anzahl Mitarbeiter pro Schicht** (en:
   "Number of employees per shift", Feld `sumEmployees`, 1–100) und **Anzahl Aufgabe pro
   Schicht** (en: "Number of tasks per shift", Feld `sumQuantity`, 1–100). Bei Containern
   ist nur die Sporadisch-Option sichtbar.
8. **Standard-Spesen** (de: "Standard-Spesen", en: "Default Expenses", fr: "Frais par
   défaut", it: "Spese predefinite") — eigene Card, nur im Experten-Modus und nicht bei
   Containern: Button **+ Neue Spese** (de: "+ Neue Spese", en: "+ New Expense", fr: "+
   Nouvelle dépense", it: "+ Nuova spesa"), Hinweis **Keine Standard-Spesen definiert**
   (de: "Keine Standard-Spesen definiert", en: "No Default Expenses defined") wenn leer,
   und eine Tabelle mit **BEZEICHNUNG** (en: "DESCRIPTION"), **BETRAG** (en: "AMOUNT",
   Zahl mit Rappen-Schritten) und **STEUERPFLICHTIG** (en: "TAXABLE", Checkbox) plus
   Papierkorb pro Zeile.

Rechts eine Filter-Spalte (ausgeblendet bei zugeschnittenen Diensten und Containern): sie
filtert die Kunden-Suche der Adresse-Card — Checkboxen Frau/Mann/juristische Person,
Gültigkeit der Mitgliedschaft (Aktive/Einstige/Zukünftige), Länder, Kantone,
Ein-/Austritts-Zeitraum und "Filter zurücksetzen".

**Bedeutung der Spezialtypen:** sporadisch = unregelmässiger Einsatz auf Abruf; TimeRange =
flexibel innerhalb eines Zeitfensters. Beide zählen NICHT in den Dienste-Balken des
Ressourcen-Monitors auf dem Dashboard.

## Verwandte Skills

- `explain_shift_sporadic` / `explain_shift_time_range` / `explain_shift_container`
- `explain_planning_assistant` — der Assistent bucht an OriginalShift/SplitShift

## Trigger-Phrasen

- "Was ist der Unterschied zwischen OriginalOrder und OriginalShift?"
- "Wann wird ein Auftrag zu einer Schicht?"
- "Warum kann ich meine Bestellung nach dem Speichern nicht mehr ändern?"
- "Wie funktioniert das Sealing eines Auftrags?"
- "How does Klacks transition from order to shift?"
- "Pourquoi ma commande est-elle verrouillée ?"
- "Cosa significa SealedOrder?"
