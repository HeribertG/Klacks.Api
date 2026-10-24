---
name: explain_shift_lifecycle_order_to_shift
description: |
  Explains the four-stage lifecycle of a shift: the editable order (Bestellung), the immutable
  sealed order, the plannable shift created by sealing, and its cut pieces; how an order or a
  customer-less duty is created; and what separates a duty billed to a customer from internal work
  or positions such as a ward shift, sealed on creation. The order entry mask itself is explained
  with the Services page. Use this when asked what an order is, how to create one or why it is not
  yet bookable.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - workflow
  de:
    - lebenszyklus
    - bestellung
    - sealen
    - bestellungen
    - was ist eine bestellung
    - bestellung erklaeren
    - auftrag
    - kundenlos
    - ohne kunde
    - dienst ohne kunde
    - interner dienst
  en:
    - lifecycle
    - sealing
    - order lifecycle
    - what is an order
    - clientless
    - without customer
    - shift without a customer
    - internal duty
  und:
    - originalorder
    - sealedorder
    - originalshift
    - splitshift
synonyms:
  de: [bestellung, auftrag, skizze, vertrag, sealen, versiegeln, schicht-schnitt, lebenszyklus, was ist ein kundenloser dienst, was bedeutet ein dienst ohne kunde, unterschied dienst mit und ohne kunde]
  en: [order, draft, contract, sealed, lifecycle, shift cut, status, what is a clientless shift, what does a shift without a customer mean, difference between billable and internal duty]
  fr: [commande, ébauche, contrat, verrouillé, cycle de vie, coupe, service sans client, difference entre service facturable et interne]
  it: [ordine, bozza, contratto, sigillato, ciclo di vita, taglio, servizio senza cliente, differenza tra servizio fatturabile e interno]
---

# Workflow Bestellung → Shift — die 4 Status-Stufen

Eine Bestellung ist der Auftrag, ein Dienst ist das, was du daraus planst: Das Versiegeln der Bestellung ist der einzige Schritt, der aus ihr einmalig den planbaren Dienst erzeugt.

<!-- level:short -->

## Stufe 1 — Bestellung und Dienst, kurz erklärt

Eine Bestellung ist ein Auftrag, der noch ausgearbeitet wird: Ein Kunde möchte zum Beispiel für den 5. Mai eine Bewachung für eine Hochzeit, und du erfasst diese Anfrage mit allen Angaben. Solange sie eine Bestellung ist, kannst du alles frei ändern, aber noch niemanden einplanen. Steht der Auftrag, versiegelst du ihn; dann ist er festgeschrieben, und es entsteht automatisch der planbare Dienst, in dem du Mitarbeitende einplanst und den du bei Bedarf in Teilstücke zerschneidest. Die Bestellung hält also fest, was vereinbart ist, der Dienst ist das, was du planst. Nicht jeder Dienst hat einen Kunden: Tanken, Putzen, Büroarbeit oder Pflege auf einer Station sind Dienste ohne Kunden, und sie entstehen direkt als fest eingetragene Dienste. Auch sie sind ganz normale bezahlte Arbeitszeit.

<!-- level:elements -->

## Stufe 2 — So gehst du in der Oberfläche vor

**Die vier Sichten der Dienste-Seite** (Navi-Icon "Alle Dienste", Radio-Auswahl in der Filter-Card rechts) zeigen die Arten von Diensten:

| Sicht | Zeigt | Wofür |
|---|---|---|
| **Bestellungen** (de: "Bestellungen", en: "Orders") | Bestellungen, die noch bearbeitbar sind; mit dem Häkchen **Nur versiegelte Bestellungen anzeigen** (de: "Nur versiegelte Bestellungen anzeigen", en: "Show Only Sealed Orders") die versiegelten Bestellungen | Aufträge erfassen und ausarbeiten |
| **Planbare Dienste** (de: "Planbare Dienste", en: "Plannable Services") | planbare Dienste und ihre zugeschnittenen Teilstücke (Unterfilter **Zeitbereich** und **Sporadisch**) | das, was im Einsatzplan gebucht wird; Schere = Zuschnitt |
| **Container** (de: "Container", en: "Container") | Container-Dienste | mehrere Aufgaben zu einem Tagespaket bündeln |
| **Abwesenheit** (de: "Abwesenheit", en: "Absence") | derzeit nichts | Platzhalter-Sicht |

Wer Dienste nicht bearbeiten darf (ohne Rolle), sieht fest nur die Sicht "Planbare Dienste".

**Eine Bestellung mit Kunde erfassen:**

1. In der Sicht "Bestellungen" auf **+ neue Bestellung** (de: "+ neue Bestellung", en: "+ New Order") klicken. Der Button erscheint nur mit dem Recht, Dienste anzulegen (Administratoren und Supervisoren).
2. In der Maske Abkürzung, Name, Von-Datum, Wochentage (oder Feiertag), mindestens eine Gruppe und den Kunden (Karte **Adresse**, de: "Adresse", en: "Address") eintragen. Mitarbeiter pro Schicht und Aufgaben pro Schicht stehen von Anfang an auf 1.
3. Solange etwas fehlt, ist der Schloss-Button in der Karte **Allgemeines** (de: "Allgemeines", en: "General") nicht aktiv. Er wird aktiv, sobald Abkürzung, Name, Von-Datum, mindestens ein Wochentag (oder Feiertag), eine Gruppe, Aufgaben pro Schicht > 0 und Mitarbeiter pro Schicht > 0 gültig sind. Tooltip: de: "Nach Sperrung ist der Auftrag unveränderlich und steht zur Planung bereit." (en: "After locking, the order is immutable and ready for planning.").
4. Schloss anklicken und speichern: Aus der Bestellung wird die versiegelte Bestellung, und der planbare Dienst entsteht automatisch. Danach zeigt die Maske ein geschlossenes Schloss und die meisten Felder sind gesperrt.

**Einen Dienst ohne Kunde anlegen:** In der Sicht "Planbare Dienste" auf **+ neuer Dienst** (de: "+ neuer Dienst", en: "+ New Service") klicken (gleiches Recht wie oben). Die Maske hat keine Kundenkarte. Der Dienst wird sofort versiegelt angelegt, nie als Entwurf, und gespeichert wird nur, wenn alle oben genannten Pflichtangaben vorhanden sind.

**Planen und zuschneiden:** Der planbare Dienst steht im Einsatzplan zum Buchen bereit. Mit der Schere in der Sicht "Planbare Dienste" öffnest du den Zuschnitt (Teilstücke nach Datum, Zeit, Wochentagen, Personal oder Aufgabe). Die Details der Maske und des Zuschnitts stehen im Skill `explain_page_shifts`.

**Löschen:** In der Sicht "Bestellungen" hat eine noch bearbeitbare Bestellung einen roten Papierkorb mit Bestätigungsdialog. Eine versiegelte Bestellung zeigt statt der Aktionen nur ein Info-Icon.

<!-- level:effects -->

## Stufe 3 — Zusammenspiel und Datenfluss

**Die vier Stufen**

(Die internen Statuswerte in Backticks sind NUR interne Anker — gegenüber Usern immer die fettgedruckten Begriffe verwenden.)

| Wert | Intern | Bedeutung |
|---|---|---|
| 0 | `OriginalOrder` | **Bestellung** (Auftrag). Frei editierbar. Nicht im Einsatzplan. Nicht buchbar. |
| 1 | `SealedOrder` | **Versiegelte Bestellung** (freigegebener Auftrag). Inhalt unveränderlich (Auftrags-Snapshot: Name, Datum, Zeit, Wochentage, Anzahl, Kunde, Typ); editierbar bleiben nur Gruppen, Standardspesen, Qualifikationen, Macro und ein noch leeres Bis-Datum. |
| 2 | `OriginalShift` | **Planbare Schicht**. Beim Versiegeln automatisch erzeugt, mit Verweis auf die versiegelte Bestellung. Schneidbar und buchbar. Gleiche Feldsperre wie bei der versiegelten Bestellung. |
| 3 | `SplitShift` | **Teilstück** (Tages-/Zeitschnitt) der planbaren Schicht. |

**Die einmalige Versiegelung**

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
│  (Status = 3)    │                          │  (Status = 2)    │
└──────────────────┘                          └──────────────────┘
```

`ShiftRepository.PutWithSealedOrderHandling` klont **nur**, wenn gilt:

```
updatedShift.Status == SealedOrder  AND  previousStatus != SealedOrder
```

Bei jedem späteren Speichern einer bereits versiegelten Bestellung entsteht kein weiterer Klon.

**Wo Mitarbeiter gebucht werden:** Der Schicht-Bereich des Einsatzplans (`get_shift_schedule`) liefert nur Dienste mit Status ≥ 2, also planbare Schichten und Teilstücke. Auf Bestellungen und versiegelte Bestellungen wird nicht gebucht.

**Dienste mit Kunde und Dienste ohne Kunde**

Der Unterschied liegt in der **Zurechenbarkeit der Arbeitsstunden**:

**Direkte Zurechnung** — Kunde → Auftrag → Dienst → Arbeitsstunden. Die Stunden sind einem *bestimmten* Auftraggeber zurechenbar, der die Leistung bestellt hat: Bewachung eines Objekts, Reinigung im Auftrag, Personalverleih an eine Firma. Fällt der Auftraggeber weg, fällt der Dienst weg.

**Keine direkte Zurechnung** — Dienst → Arbeitsstunden. Die Stunden gehören zum Betrieb, nicht zu einem Auftraggeber. Drei typische Formen:

1. **Versteckter Teil der Dienstleistung** — Arbeiten, die zwar durch die Aufträge verursacht werden, aber keinem einzelnen zurechenbar sind (Gemeinkosten): Tanken, Autopflege, Putzen.
2. **Innendienst** — Büro, Verwaltung, Disposition.
3. **Ganze Berufsbilder** — Pflege auf einer Station, Koch in der Küche, Coiffeur im Salon. Hier gibt es im Geschäftsmodell **gar keinen** Auftraggeber, dem ein einzelner Dienst zugerechnet würde — auch keinen indirekten. Wechselnde Patienten oder Gäste sind keine Auftraggeber im Sinne eines Auftrags.

In einem Spital, einem Restaurant oder einem Salon ist der kundenlose Dienst damit **der Regelfall, nicht die Ausnahme**. Ein Dienst ohne Kunde ist deshalb **nie** als unvollständig erfasster Datensatz zu deuten.

**Für die Arbeitsstunden macht es keinen Unterschied:** Die Arbeitszeit zählt in beiden Fällen, denn der Mitarbeiter hängt am Einsatz, der Kunde am Dienst. Die Zurechnung entscheidet nicht, *ob* Stunden zählen, sondern **wem** sie zugeordnet werden können. Technisch ist die Zurechnung das Feld `Shift.ClientId`: gesetzt heisst direkt zurechenbar, leer heisst nicht zurechenbar.

**Wie ein kundenloser Dienst entsteht:** Über den Button "+ neuer Dienst" (siehe Stufe 2); die Kundenkarte wird dort gar nicht erst angezeigt, die fehlende Zurechnung ist damit eine bewusste Entscheidung. Der Dienst wird **sofort versiegelt** angelegt, nie als Entwurf, denn `OriginalOrder` heisst "Anfrage eines Kunden, die noch ausgearbeitet wird" — ein Entwurf ohne Kunde wäre von einem Entwurf, bei dem der Kunde noch fehlt, nicht zu unterscheiden. Daraus folgen zwei feste Regeln, die der Server beim Anlegen erzwingt:

- Eine Bestellung im Status `OriginalOrder` **ohne** Kunde wird abgelehnt.
- Ein kundenloser Dienst wird sofort versiegelt und muss deshalb schon beim Anlegen vollständig sein (Abkürzung, Name, Startdatum, mindestens ein Wochentag oder Feiertag, mindestens eine Gruppe, Anzahl Aufgaben > 0, Mitarbeiterzahl > 0).

Container sind davon nicht betroffen: sie sind Vorlagen, keine Bestellungen, und tragen nie einen Kunden.

**Bestellungen über Klacksy anlegen und versiegeln:** Eine Bestellung, die Klacksy anlegt (`create_shift`), entsteht immer als offene **Bestellung** (`OriginalOrder`), nie direkt versiegelt. Versiegelt wird sie in einem eigenen Schritt mit `seal_shift`: Klacksy zeigt zuerst eine Vorschau der Bestellung, und versiegelt wird erst, wenn du diese Vorschau in deiner nächsten Antwort ausdrücklich bestätigst; erst danach entsteht der planbare Dienst, der gebucht und zugeschnitten werden kann. Klacksy versiegelt nie mehrere Bestellungen auf einmal. Weil eine Bestellung ohne Gruppe nicht versiegelbar ist, leitet der Skill `assign_orders_to_groups` die Gruppe jeder noch gruppenlosen Bestellung aus der Adresse ihres Kunden ab (Ort, sonst Kanton, sonst nächstgelegene Gruppe mit Koordinaten).

**Beispiel-Lebenszyklus**

```
2026-05-01  Anwender: neuer Shift "Hochzeit Müller" (OriginalOrder).
            Iterative Verfeinerung der Details.

2026-05-03  Kunde bestätigt → Status=SealedOrder + Save.
            Backend: SealedOrder festgeschrieben (id=A) + Klon OriginalShift
            (id=B, OriginalId=A) erzeugt.

2026-05-04  Disposition öffnet id=B, trägt 2 Mitarbeiter ein.

2026-05-05  Anwender zerschneidet id=B in 2 SplitShifts (id=C, id=D).

2026-06-01  Periode geschlossen → LockLevel=Closed auf den Works auf C+D.
            id=A (SealedOrder) ist nie verändert worden — bleibt als Vertrag.
```

**Orthogonales Locking:** Einzelne `Work`/`Break`-Einträge tragen ein separates `LockLevel` (None / Confirmed / Approved / Closed). Das ist **NICHT** zu verwechseln mit `ShiftStatus.SealedOrder`. Sealing eines Works ist Entry-Level, Sealing eines Shifts ist Lifecycle-Level.

**Die Erfassungsmaske** (`/workplace/new-shift`, bearbeiten via `/workplace/edit-shift/:id`): Dienste mit Kunde entstehen über den Button **+ neue Bestellung** als Bestellung; der Button **+ neuer Dienst** (Sicht "Planbare Dienste") legt einen Dienst ohne Kunde an, der direkt als versiegelte Bestellung entsteht. Details der Erfassungsmaske (alle Cards, Felder, Lock-Button, Experten-Modus, Filter-Spalte): Skill `explain_page_shifts`.

## Verwandte Skills

- `explain_shift_sporadic` / `explain_shift_time_range` / `explain_shift_container`
- `explain_planning_assistant` — der Assistent bucht an OriginalShift/SplitShift
- `assign_orders_to_groups` — leitet die fehlenden Gruppen importierter Bestellungen aus der Kundenadresse ab
- `seal_shift` — versiegelt eine einzelne Bestellung, erst nachdem du ihre Vorschau bestätigt hast

## Trigger-Phrasen

- "Was ist der Unterschied zwischen OriginalOrder und OriginalShift?"
- "Wann wird ein Auftrag zu einer Schicht?"
- "Warum kann ich meine Bestellung nach dem Speichern nicht mehr ändern?"
- "Wie funktioniert das Sealing eines Auftrags?"
- "How does Klacks transition from order to shift?"
- "Pourquoi ma commande est-elle verrouillée ?"
- "Cosa significa SealedOrder?"

<!-- level:pitfalls -->

## Stufe 4 — Folgen, Endgültiges, Korrigierbares

**Endgültig**

- **Versiegeln ist nicht umkehrbar.** Der Status gehört selbst zu den gesperrten Feldern; es gibt keinen Weg zurück zur Bestellung. Die einzige Korrektur ist Löschen und Neuanlegen (siehe unten).
- **Kunde und Typ sind nach dem Versiegeln fest.** Auch der Kunde (`ClientId`) und der Dienst-Typ (Aufgabe oder Container), ebenso Zeiten, Wochentage, Anzahl, Sporadik- und Zeitfenster-Einstellung. Ein kundenloser Dienst bekommt also nachträglich keinen Kunden und umgekehrt.
- **Planbare Schichten und Teilstücke sind nie direkt löschbar.** Der Server lehnt das mit einem Konflikt (409) ab; auch die Maske bietet es nicht an.
- **Ein Dienst ohne Kunde muss vollständig sein, bevor er angelegt wird.** Er wird sofort versiegelt; fehlt eine Pflichtangabe, wird er nicht gespeichert.

**Korrigierbar**

- **Gruppen, Standardspesen, Qualifikationen und Macro** bleiben auch nach dem Versiegeln änderbar.
- **Leeres Bis-Datum:** Hat eine versiegelte Bestellung kein Bis-Datum (z. B. weil sich der Auftrag regelmäßig verlängert), lässt es sich einmalig nachtragen; danach ist es gesperrt wie alle anderen Felder. Setzt du es über Klacksy (`set_sealed_order_until_date`), wird zusätzlich geprüft, dass nach dem Datum nichts verplant ist; die planbare Schicht und die Teilstücke behalten dabei ihr eigenes Bis-Datum und werden nicht nachgeführt.
- **Zuschnitt zurücksetzen:** Die Schaltfläche **Zurücksetzen** im Zuschnitt (de: "Zurücksetzen", en: "Reset") schließt die noch offenen Teilstücke am Tag vor einem neuen Startdatum und legt ab diesem Datum eine neue ungeteilte planbare Schicht aus der Bestellung an; frühere Schnitte bleiben unberührt, das neue Startdatum muss innerhalb der Gültigkeit der Bestellung liegen. So endet auch eine Schnittgruppe, die sich nicht löschen lässt.
- **Löschen:** Eine noch bearbeitbare Bestellung lässt sich löschen (Papierkorb). Eine versiegelte Bestellung kann nur ein Administrator löschen, über Klacksy (Skill `delete_shift`) oder die API, samt planbarer Schicht und Teilstücken — und nur, solange in der Familie nichts verplant ist: keine Einsätze, keine Szenario-Kopien, keine Container-Verweise. Sonst wird abgelehnt. In der Dienste-Liste gibt es dafür keinen Papierkorb.
- **Im Szenario sind Zuschnitt-Aktionen gesperrt.** Zum Zuschneiden oder Zurücksetzen das Szenario zuerst übernehmen oder verwerfen.

**Grenzfälle**

- **Beim Anlegen wird ein Entwurf ohne Kunde abgelehnt.** Ausnahme sind Container: sie sind Vorlagen und tragen nie einen Kunden.
- **Klacksy versiegelt nie beim Anlegen und nie im Stapel.** Jede Versiegelung über Klacksy betrifft genau eine Bestellung und läuft erst, nachdem du ihre Vorschau bestätigt hast; eine Bestellung ohne Gruppe lässt sich nicht versiegeln, und was versiegelt ist, bleibt es.
- **Dieselbe Bezeichnung, drei Dinge:** "Versiegeln" einer Bestellung (Stufenwechsel des Dienstes) ist nicht dasselbe wie die Sperrstufe eines Einsatzes (de: "Bestätigt", "Freigegeben", "Geschlossen"; en: "Confirmed", "Approved", "Closed"; fr: "Confirmé", "Approuvé", "Clôturé"; it: "Confermato", "Approvato", "Chiuso") und nicht die Tagessperre.
