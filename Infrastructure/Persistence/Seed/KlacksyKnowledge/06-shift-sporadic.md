---
name: explain_shift_sporadic
description: |
  Explains the Sporadic Shift in Klacks. A sporadic shift may only occur a
  limited number of times per fixed period (Week/Month/Quarter/Semester/Year/
  ContractualTerm). SumEmployees caps parallel bookings per day, Quantity caps
  distinct booked days per period. Once either cap is reached, the remaining
  days in the period are visually blocked (opacity 0.3, not draggable). Use
  this when the user asks why a shift is blocked, what SporadicScope means, or
  how SumEmployees and Quantity interact.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - sporadic
  - sporadisch
  - scope
  - quantity
  - sumemployees
  - bezugsperiode
  - blocked
  - gesperrt
synonyms:
  de: [sporadisch, gelegentlich, bezugsperiode, periode, periodisch, kapazität, sperrung]
  en: [sporadic, occasional, scope, period, capacity, blocked, locked]
  fr: [sporadique, occasionnel, période, capacité, bloqué]
  it: [sporadico, occasionale, periodo, capacità, bloccato]
---

# Sporadic Shift — periodisch begrenzte Häufigkeit

Ein sporadischer Dienst findet innerhalb einer festen Periode (Woche, Monat, Quartal, Semester, Jahr oder Vertragslaufzeit) nur begrenzt oft statt.

<!-- level:short -->

## Stufe 1 — Wofür ist das?

Ein sporadischer Dienst ist ein Dienst, den du nur gelegentlich brauchst, zum Beispiel eine Grundreinigung, die pro Woche an höchstens drei Tagen stattfinden soll, jeweils mit zwei Personen. Du legst beim Anlegen des Dienstes fest, in welcher Periode gezählt wird (zum Beispiel pro Woche oder pro Monat), an wie vielen Tagen der Dienst in dieser Periode vorkommen darf und wie viele Personen an einem Tag eingeplant werden dürfen. Sind alle erlaubten Tage vergeben, werden die übrigen Tage dieser Periode im Einsatzplan abgeblendet, und der Dienst lässt sich dort nicht mehr buchen. So verhinderst du, dass ein Dienst, der nur gelegentlich nötig ist, versehentlich zu oft eingeplant wird.

<!-- level:elements -->

## Stufe 2 — So legst du ihn an und erkennst ihn

**Anlegen** (Dienste-Seite, Navi-Icon "Alle Dienste"):

1. In der Sicht "Bestellungen" (de: "Bestellungen", en: "Orders") auf **+ neue Bestellung** (de: "+ neue Bestellung", en: "+ New Order") klicken. Der Button erscheint nur mit dem Recht, Dienste anzulegen (Administratoren und Supervisoren).
2. In der Maske den Schalter **Experten Modus** (de: "Experten Modus", en: "Expert Mode") einschalten — erst dann erscheint die Karte **Spezielle Merkmale** (de: "Spezielle Merkmale", en: "Special Features").
3. Dort das Häkchen **sporadischer Einsatz** (de: "sporadischer Einsatz", en: "Sporadic use") setzen und im Dropdown daneben die Periode wählen: de: "Wöchentlich", de: "Monatlich", de: "Quartal", de: "Semester", de: "Jährlich" oder de: "Laufzeit des Vertrages".
4. **Anzahl Mitarbeiter pro Schicht** (de: "Anzahl Mitarbeiter pro Schicht", en: "Number of employees per shift") = wie viele Personen an einem Tag gleichzeitig gebucht werden dürfen. **Anzahl Aufgabe pro Schicht** (de: "Anzahl Aufgabe pro Schicht", en: "Number of tasks per shift") = bei einem sporadischen Dienst an wie vielen verschiedenen Tagen pro Periode er gebucht werden darf.
5. Zum Planen muss die Bestellung wie jede andere versiegelt werden (Schloss-Button in der Karte **Allgemeines**, de: "Allgemeines", en: "General"); der Dienst erscheint dann in der Sicht "Planbare Dienste" (de: "Planbare Dienste", en: "Plannable Services").

**Erkennen in der Dienstliste:** sporadische Dienste tragen das Fragezeichen-Uhr-Icon. In der Sicht "Planbare Dienste" lassen sie sich mit der Checkbox **Sporadisch** (de: "Sporadisch", en: "Sporadic") ein- und ausblenden.

**Erkennen im Einsatzplan** (Schicht-Bereich mit den Diensten): jede Zelle hat einen von drei Zuständen.

| Zustand | Bedingung | Darstellung |
|---|---|---|
| Frei | weder der Tag noch die Periode ist voll | normal, buchbar |
| Gebucht | am Tag sind so viele Personen gebucht wie erlaubt | als belegt markiert, nicht ziehbar |
| Gesperrt | am Tag ist noch niemand gebucht, aber die Periode hat ihre erlaubten Tage schon verbraucht | belegt markiert und abgeblendet, nicht ziehbar |

Plaketten in der Zelle: oben links "gebucht/erlaubt pro Tag" (nur wenn mehr als eine Person erlaubt ist), oben rechts "gebuchte Tage/erlaubte Tage der Periode" (nur wenn mehr als ein Tag erlaubt ist).

**Buchen:** wie jeden Dienst — in eine freie Zelle ziehen, per Kontextmenü oder per Kürzel eintippen (Details auf der Einsatzplan-Seite). Gebuchte und gesperrte Zellen lassen sich nicht ziehen; versuchst du trotzdem zu buchen, lehnt der Server mit einer Meldung ab (siehe Zusammenspiel).

<!-- level:effects -->

## Stufe 3 — Zusammenspiel und Datenfluss

**Felder und ihre Wirkung**

| Feld | Wirkung |
|---|---|
| `IsSporadic = true` | Aktiviert die Periodenlogik. |
| `SporadicScope` | Periodenlänge: `Week`, `Month`, `Quarter`, `Semester`, `Year`, `ContractualTerm`. |
| `SumEmployees` | **Pro Tag** maximal so viele Buchungen parallel. |
| `Quantity` | **Pro Periode** maximal so viele verschiedene Buchungstage. |

Maximal mögliche Buchungen pro Periode = `SumEmployees × Quantity`.

**Beispiel:** Periode "Wöchentlich" (Scope=Week), SumEmployees=2, Quantity=3 → höchstens 3 verschiedene Tage mit je 2 Personen = 6 Buchungen pro Woche. Sind Montag, Mittwoch und Freitag voll, sind Dienstag, Donnerstag, Samstag und Sonntag dieser Woche gesperrt.

**Woher der Zustand kommt:** Die Datenbank-Abfrage des Schicht-Bereichs (`get_shift_schedule`) berechnet pro Dienst und Tag den Zustand (0 = frei, 1 = gebucht, 2 = gesperrt) und die Anzahl der in der Periode schon gebuchten Tage; der Einsatzplan zeichnet ihn nur.

**Serverprüfung beim Buchen:** Beim Anlegen eines Einsatzes und beim Wiederherstellen eines gelöschten Einsatzes prüft der Server (`WorkWriteGuardService`) beide Grenzen und lehnt mit einem Konflikt (HTTP 409) ab, wenn

- der Tag schon `SumEmployees` Buchungen hat **oder**
- die Periode schon `Quantity` verschiedene Buchungstage hat und der gewünschte Tag noch leer ist.

Die Oberfläche zeigt dazu eine Meldung mit Dienstname, Datum und Zählerstand.

**Kombinationen:** Ein Dienst kann gleichzeitig Zeitfenster-Dienst sein (dann bestimmt das Zeitfenster, WANN am Tag gearbeitet wird, die Sporadik, WIE OFT) und ein Container kann sporadisch sein (die Grenzen gelten dann für die Container-Buchung).

**Dashboard:** Sporadische Dienste zählen nicht im Dienste-Balken des Ressourcen-Monitors.

## Verwandte Skills

- `explain_shift_time_range` — Sporadic + TimeRange sind kombinierbar
- `explain_shift_container` — Container können sporadisch sein
- `explain_shift_lifecycle_order_to_shift` — Bestellung, Versiegeln und planbarer Dienst
- `explain_page_shifts` — die Dienste-Seite und die Erfassungsmaske

## Trigger-Phrasen

- "Was ist ein sporadischer Dienst?"
- "Warum ist mein Dienst gesperrt obwohl noch niemand gebucht ist?"
- "Wie wirkt die Periode Wöchentlich bei einem sporadischen Dienst?"
- "What does sporadic shift mean in Klacks?"
- "Pourquoi ce service est-il bloqué cette semaine ?"
- "Cosa significa turno sporadico?"

<!-- level:pitfalls -->

## Stufe 4 — Folgen, Grenzfälle, Endgültiges

- **Nach dem Versiegeln sind die Grenzen fest.** Sporadik-Haken, Periode, Mitarbeiter pro Tag und Tage pro Periode gehören zu den gesperrten Feldern einer versiegelten Bestellung und ihrer planbaren Schicht. Ist eine Grenze falsch, hilft nur Löschen und neu anlegen: eine versiegelte Bestellung kann ein Administrator über Klacksy oder die API löschen, solange nichts darin verplant ist; die Maske bietet das nicht an.
- **Die Periode ist ein Kalenderfenster, kein gleitendes.** Woche = Montag bis Sonntag, Monat/Quartal/Jahr = Kalenderzeitraum, Semester = Januar–Juni bzw. Juli–Dezember. "Laufzeit des Vertrages" nimmt Von- und Bis-Datum des Dienstes selbst; ohne Bis-Datum ist die Periode offen. Ein Wert 0 bei Mitarbeitern oder Tagen wird wie 1 behandelt.
- **Teilweise belegte Tage bleiben buchbar.** Gesperrt werden nur LEERE Tage, sobald die Periode ihre erlaubten Tage verbraucht hat; ein Tag mit schon einer Buchung nimmt weitere Personen bis zur Tagesgrenze auf.
- **Gezählt werden nur eigenständige Einsätze.** Einsätze, die ein Container beim Buchen automatisch erzeugt (Unter-Einsätze), zählen nicht mit. Ein sporadischer Aufgaben-Dienst innerhalb eines Containers verbraucht deshalb keine sporadische Kapazität.
- **Szenarien zählen getrennt.** Im Szenario werden nur die Einsätze desselben Szenarios gezählt, im echten Plan nur echte Einsätze.
- **Löschen gibt Kapazität frei — Wiederherstellen prüft neu.** Wird in der Zwischenzeit ein anderer Einsatz gebucht, kann das Wiederherstellen des alten mit einem Konflikt scheitern.
- **Nicht jeder Weg wird serverseitig geprüft.** Einzelbuchung und Wiederherstellen werden vom Server abgelehnt; die Sammel-Buchung beim Einfügen mehrerer Zellen läuft über einen anderen Weg und prüft die sporadischen Grenzen dort nicht. Achte beim Einfügen selbst auf abgeblendete Tage.
- **Gebucht und gesperrt sehen ähnlich aus.** Beide sind nicht ziehbar; nur gesperrte Zellen sind zusätzlich abgeblendet. Der Unterschied: "gebucht" heißt, dass dieser Tag selbst voll ist, "gesperrt" heißt, dass die Periode erschöpft ist.
