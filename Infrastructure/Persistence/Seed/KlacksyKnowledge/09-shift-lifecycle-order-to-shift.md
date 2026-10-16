---
name: explain_shift_lifecycle_order_to_shift
description: |
  Explains the four-stage lifecycle of a shift in Klacks — from the editable
  order via the immutable sealed order to the active plannable shift and its
  cut pieces — and how an order or a customer-less duty is created (the
  card-by-card walkthrough of the order entry mask lives in
  explain_page_shifts).
  Use this when the user asks what an order is, how to create one, why an
  order is not yet bookable, or what
  separates a duty billed to a customer from one that has none (internal work
  such as refuelling or cleaning, and positions such as a ward, kitchen or
  salon shift).
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
  - kundenlos
  - ohne kunde
synonyms:
  de: [bestellung, auftrag, skizze, vertrag, sealen, versiegeln, schicht-schnitt, lebenszyklus, dienst ohne kunde, kundenloser dienst, interner dienst]
  en: [order, draft, contract, sealed, lifecycle, shift cut, status, shift without customer, clientless shift, internal duty]
  fr: [commande, ébauche, contrat, verrouillé, cycle de vie, coupe, service sans client, service interne]
  it: [ordine, bozza, contratto, sigillato, ciclo di vita, taglio, servizio senza cliente, servizio interno]
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
| 1 | `SealedOrder` | **Versiegelte Bestellung** (freigegebener Auftrag). Inhalt unveränderlich (Auftrags-Snapshot: Name, Datum, Zeit, Wochentage, Anzahl, Adresse, Typ); editierbar bleiben nur Gruppen, Standardspesen, Qualifikationen, Macro und ein noch leeres Bis-Datum. Zusätzlich kann ein Admin die versiegelte Bestellung samt Familie löschen, solange darin nichts verplant ist (siehe unten). |
| 2 | `OriginalShift` | **Planbare Schicht**. Beim Versiegeln automatisch erzeugt, mit Verweis auf die versiegelte Bestellung. Schneidbar und buchbar. Der Inhalt ist wie bei der versiegelten Bestellung gesperrt (Name, Datum, Zeit, Wochentage, Anzahl, Adresse, Typ); editierbar bleiben nur Gruppen, Standardspesen, Qualifikationen, Macro und ein noch leeres Bis-Datum. |
| 3 | `SplitShift` | **Teilstück** (Tages-/Zeitschnitt) der planbaren Schicht. |

**Ausnahme bei den gesperrten Feldern — nachträgliches Bis-Datum:** Hat eine
versiegelte Bestellung KEIN Bis-Datum (z. B. weil sich der Auftrag regelmässig verlängert
oder sein Ende noch nicht definiert ist), kann das Bis-Datum auch nach dem Versiegeln noch
gesetzt werden — als einziges der gesperrten Felder. Voraussetzung: Ab diesem Datum dürfen noch
keine Dienste verplant sein. Sobald ein Bis-Datum gesetzt ist, ist auch dieses Feld
gesperrt wie alle anderen.

**Löschen durch einen Admin:** Eine versiegelte Bestellung kann ein Admin
samt ihrer Familie (planbare Schicht und Teilstücke) löschen, solange in der Familie nichts
verplant ist: keine Einsätze, keine Szenario-Klone und keine Container-Referenzen. Sonst wird
das Löschen abgelehnt. Planbare Schichten und Teilstücke selbst sind nicht löschbar. In der
Dienste-Liste zeigen versiegelte Bestellungen keinen Papierkorb (nur ein Info-Icon); dieses
Löschen läuft über den Assistenten Klacksy (Skill `delete_shift`) bzw. die API.

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

## Ganze Bestellungs-Stapel versiegeln (z. B. nach einem ERP-Import)

Der Skill `seal_open_orders` versiegelt alle offenen Bestellungen eines Filters
(Quellsystem, Zeitraum, Kunde, Gruppe) auf einmal — jede in ihrer eigenen
Transaktion, blockierte und fehlgeschlagene Bestellungen werden einzeln
ausgewiesen statt den ganzen Lauf abzubrechen; mit `apply=false` ist es nur eine
Vorschau, und Versiegeln lässt sich danach nicht mehr rückgängig machen. Betrifft
der Lauf mehr als 25 versiegelbare Bestellungen, läuft er als Hintergrund-Auftrag:
Klacksy antwortet sofort mit einer Auftrags-Id, und das Ergebnis (versiegelt/
blockiert/fehlgeschlagen, Dauer) landet als Meldung im Klacksy-Postfach, sobald
der Auftrag fertig ist — bis dahin bleibt der Zustand der noch nicht bearbeiteten
Bestellungen unverändert.
Weil eine Bestellung ohne Gruppe nicht versiegelbar ist und der ERP-Import
Bestellungen ohne Gruppe anlegt, leitet der Skill `assign_orders_to_groups` die
Gruppe jeder noch gruppenlosen Bestellung aus der Adresse ihres Kunden ab (Ort,
sonst Kanton, sonst nächstgelegene Gruppe mit Koordinaten) — entweder vorab als
eigener Aufruf oder direkt über den Parameter `autoAssignGroups`.

## Wo Mitarbeiter gebucht werden

`Work`-Entitäten hängen **ausschließlich** an Shifts mit Status ≥ 2 — also
`OriginalShift` oder `SplitShift`. Versuche, auf `OriginalOrder` oder
`SealedOrder` zu buchen, sind technisch und semantisch ausgeschlossen.

## Dienste mit Kunde und Dienste ohne Kunde

Der Unterschied liegt in der **Zurechenbarkeit der Arbeitsstunden**:

**Direkte Zurechnung** — Kunde → Auftrag → Dienst → Arbeitsstunden. Die
Stunden sind einem *bestimmten* Auftraggeber zurechenbar, der die Leistung
bestellt hat: Bewachung eines Objekts, Reinigung im Auftrag, Personalverleih
an eine Firma. Fällt der Auftraggeber weg, fällt der Dienst weg.

**Keine direkte Zurechnung** — Dienst → Arbeitsstunden. Die Stunden gehören
zum Betrieb, nicht zu einem Auftraggeber. Drei typische Formen:

1. **Versteckter Teil der Dienstleistung** — Arbeiten, die zwar durch die
   Aufträge verursacht werden, aber keinem einzelnen zurechenbar sind
   (Gemeinkosten): Tanken, Autopflege, Putzen.
2. **Innendienst** — Büro, Verwaltung, Disposition.
3. **Ganze Berufsbilder** — Pflege auf einer Station, Koch in der Küche,
   Coiffeur im Salon. Hier gibt es im Geschäftsmodell **gar keinen**
   Auftraggeber, dem ein einzelner Dienst zugerechnet würde — auch keinen
   indirekten. Wechselnde Patienten oder Gäste sind keine Auftraggeber im
   Sinne eines Auftrags.

In einem Spital, einem Restaurant oder einem Salon ist der kundenlose Dienst
damit **der Regelfall, nicht die Ausnahme** — dort trägt der gesamte
Dienstplan keinen Kunden. Ein Dienst ohne Kunde ist deshalb **nie** als
unvollständig erfasster Datensatz zu deuten.

**Für die Arbeitsstunden macht es keinen Unterschied.** Tanken und
Stationsdienst sind bezahlte Arbeitszeit und fliessen genauso in Soll/Ist,
Lohn, Zuschläge und Ruhezeiten ein wie ein Kundendienst. Klacks selbst stellt
keine Kundenrechnungen — es plant Dienste und rechnet Arbeitsstunden. Die
Zurechnung entscheidet also nicht, *ob* Stunden zählen, sondern **wem** sie
zugeordnet werden können.

Technisch ist die Zurechnung genau das Feld `Shift.ClientId`: gesetzt heisst
direkt zurechenbar, leer heisst nicht zurechenbar. Damit es leer nur mit
Absicht sein kann, gelten die Regeln im nächsten Abschnitt.

### Wie ein kundenloser Dienst entsteht

In der Ansicht **Planbare Dienste** gibt es für Administratoren einen eigenen
Button "Neuer Dienst". Die Kundenkarte wird dort gar nicht erst angezeigt, ein
Kunde ist also nicht setzbar — die fehlende Zurechnung ist damit eine bewusste
Entscheidung des Nutzers und kein Versehen.

Ein solcher Dienst wird **sofort versiegelt** angelegt, nie als Entwurf. Der
Grund ist die Aussage des Status selbst: `OriginalOrder` heisst "Anfrage eines
Kunden, die noch ausgearbeitet wird" — ein Entwurf ohne Kunde wäre von einem
Entwurf, bei dem der Kunde noch fehlt, nicht zu unterscheiden. Daraus folgen
zwei feste Regeln:

- Eine Bestellung im Status `OriginalOrder` **ohne** Kunde wird abgelehnt.
- Ein kundenloser Dienst wird sofort versiegelt und muss deshalb schon beim
  Anlegen vollständig sein (Kürzel, Name, Startdatum, mindestens ein Wochentag
  oder Feiertag, mindestens eine Gruppe, Anzahl > 0, Mitarbeiterzahl > 0) —
  Versiegeln ist unumkehrbar.

Container sind davon nicht betroffen: sie sind Vorlagen, keine Bestellungen,
und tragen ohnehin nie einen Kunden.

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

Dienste mit Kunde entstehen über den Button **+ neue Bestellung** auf der Dienste-Seite als
Bestellung; der Button **+ neuer Dienst** (Sicht "Planbare Dienste") legt einen Dienst ohne
Kunde an, der direkt als versiegelte Bestellung entsteht. Beide öffnen diese Maske. Details der
Erfassungsmaske (alle Cards, Felder, Lock-Button, Experten-Modus, Filter-Spalte): Skill
`explain_page_shifts`.

## Verwandte Skills

- `explain_shift_sporadic` / `explain_shift_time_range` / `explain_shift_container`
- `explain_planning_assistant` — der Assistent bucht an OriginalShift/SplitShift
- `assign_orders_to_groups` — leitet die fehlenden Gruppen importierter Bestellungen aus der Kundenadresse ab
- `seal_open_orders` — versiegelt alle offenen Bestellungen eines Filters in einem Lauf

## Trigger-Phrasen

- "Was ist der Unterschied zwischen OriginalOrder und OriginalShift?"
- "Wann wird ein Auftrag zu einer Schicht?"
- "Warum kann ich meine Bestellung nach dem Speichern nicht mehr ändern?"
- "Wie funktioniert das Sealing eines Auftrags?"
- "How does Klacks transition from order to shift?"
- "Pourquoi ma commande est-elle verrouillée ?"
- "Cosa significa SealedOrder?"
