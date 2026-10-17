---
name: explain_why_not_visible
description: |
  Explains why a person or a shift is missing from a list or from the schedule. Covers the group chosen at
  the top of the app, the root groups a user may see, the validity filter (active, former, future), entry
  and exit dates, the employee and external checkboxes, an active search text, deleted persons behind the
  admin-only "deleted addresses" filter, ungrouped shifts, deleted groups, customers never appearing as
  schedule rows, and scenario copies of shifts.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - nicht sichtbar
    - fehlt in der liste
    - verschwunden
    - person gelöscht
  en:
    - not visible
    - missing from the list
    - disappeared
    - deleted person
synonyms:
  de: [warum taucht die neue mitarbeiterin nicht in der liste auf, wo ist der kollege im einsatzplan hin, ich finde den dienst im plan nicht mehr, wieso sehe ich nur einen teil der leute, der ausgetretene mitarbeiter ist weg, eine gelöschte person wieder anzeigen, warum ist die zeile im nächsten monat verschwunden, wo sind die dienste ohne gruppe]
  en: [why doesn't the new hire show up in the list, where did my colleague go in the schedule, i can't find the shift in the plan any more, why do i only see some of the staff, the person who left is gone, show a deleted person again, why did the row vanish next month, where are the shifts without a group]
  fr: [pourquoi la nouvelle employée n'apparaît pas dans la liste, où est passé le collègue dans le planning, je ne trouve plus le service dans le plan, pourquoi je ne vois qu'une partie du personnel, l'employé parti a disparu, afficher à nouveau une personne supprimée, où sont les services sans groupe]
  it: [perché la nuova dipendente non compare nell'elenco, dov'è finito il collega nella pianificazione, non trovo più il turno nel piano, perché vedo solo una parte del personale, il dipendente uscito è sparito, mostrare di nuovo una persona eliminata, dove sono i turni senza gruppo]
---

# Warum sehe ich die Person oder den Dienst nicht?

<!-- level:short -->

## Stufe 1 — Kurz gesagt

Fehlt eine Person oder ein Dienst, ist er meistens nur ausgeblendet, nicht weg. Die häufigsten Gründe: Oben in
der App ist eine Gruppe gewählt, ein Suchtext steht noch im Suchfeld, oder die Liste zeigt ab Werk nur
**aktive** Personen — wer erst später eintritt oder schon ausgetreten ist, steht unter einem anderen Filter.
Wer kein Admin ist, sieht ausserdem nur Personen aus den Gruppen, die ein Admin für ihn freigegeben hat (plus
Personen ganz ohne Gruppe). Gelöschte Personen kann nur ein Admin anzeigen, zurückholen lassen sie sich nicht.
Sag mir, wen oder was du suchst und auf welcher Seite, dann prüfen wir die Gründe der Reihe nach.

<!-- level:elements -->

## Stufe 2 — Gründe, Erkennen, Abhilfe

| Grund | Woran erkennbar | Abhilfe | Details |
|---|---|---|---|
| **Gruppe gewählt** | In der Kopfleiste steht eine Gruppe statt (de: "Alle Gruppen", en: "All groups", fr: "Tous les groupes", it: "Tutti i gruppi") | Gruppe wechseln oder alle zeigen; die Wahl gilt für alle Arbeitsbereich-Seiten gleichzeitig | `explain_page_groups` |
| **Nicht freigegebene Gruppe** (nur Nicht-Admins) | Person existiert, ist aber in keiner der sichtbaren Wurzelgruppen | Ein Admin hakt die Wurzelgruppe im Dialog (de: "Sichtbare Rootgruppe", en: "Visible Root Group", fr: "Groupe racine visible", it: "Gruppo radice visibile") an | `explain_group_visibility_blind_spots` |
| **Suchtext aktiv** | Suchfeld in der Kopfleiste ist nicht leer | Suche leeren | `explain_page_schedule` |
| **Gültigkeits-Filter** (Personenliste) | Filter (de: "Gültigkeit", en: "Validity", fr: "Validité", it: "Validità") steht nur auf (de: "Aktive", en: "Active", fr: "Actifs", it: "Attivi") | Künftige Eintritte unter (de: "Zukünftige", en: "Future", fr: "Futurs", it: "Futuri"), Ausgetretene unter (de: "Ehemalige", en: "Alumni", fr: "Anciens élèves", it: "Ex-allievi") | `explain_page_employees` |
| **Mitgliedschaft passt nicht zum Zeitraum** (Plan) | Eine Zeile braucht eine Mitgliedschaft, die den angezeigten Zeitraum überlappt; in einer Periode ganz vor dem Eintritt oder ganz nach dem Austritt fehlt die Zeile | Eintritts- und Austrittsdatum prüfen | `explain_address_management` |
| **Typ ausgeblendet** (Plan) | Im Filter-Popup des Zeilenkopfs ist (de: "Mitarbeiter", en: "Employees", fr: "Employés", it: "Dipendenti") oder (de: "Extern", en: "Ext", fr: "Ext", it: "Est") nicht angehakt | Haken wieder setzen | `explain_page_schedule` |
| **Kunde gesucht** (Plan) | Kunden sind nie Zeilen im Einsatzplan, nur eigene und externe Mitarbeitende | Kunden in der Personenliste suchen | `explain_page_schedule` |
| **Gelöscht** (nur Admins) | Person erscheint nur mit (de: "Gelöschte Adressen", en: "Deleted Addresses", fr: "Adresses supprimées", it: "Indirizzi Eliminati"); für Nicht-Admins wird der Filter ignoriert | Ansehen ja (schreibgeschützt), wiederherstellen nein | `explain_page_employees` |
| **Dienst ohne Gruppe** (Plan) | Dienst hat keine Gruppenzuordnung, und oben ist eine Gruppe gewählt oder der Benutzer ist kein Admin | Im Schicht-Filter (de: "Nicht gruppierte Dienste", en: "Ungrouped Shifts", fr: "Services non groupés", it: "Turni non raggruppati") anhaken oder dem Dienst eine Gruppe geben | `explain_group_model` |
| **Szenario-Kopie** | Gesucht wird die Szenario-Kopie eines Dienstes; die Dienste-Seite zeigt nur die echten Dienste, Szenario-Kopien bleiben dort unsichtbar | Das Szenario im Einsatzplan öffnen; in den echten Plan kommen seine Änderungen erst, wenn das Szenario übernommen wird | `explain_page_shifts` |

<!-- level:effects -->

## Stufe 3 — Zusammenspiel

- **Zwei Filter übereinander** (Personen): Die Gruppen-Auswahl oben wirkt immer innerhalb der freigegebenen
  Gruppen eines Benutzers, nie darum herum. Eine Person ist sichtbar, sobald eine ihrer Gruppen sichtbar ist. Admins sind nie
  eingeschränkt.
- **Untergruppen**: In den Personenlisten und im Einsatzplan zeigt die gewählte Gruppe ihre Untergruppen mit.
  Eine Untergruppe lässt sich aber nicht einzeln freigeben, nur die ganze Wurzelgruppe.
- **Dienste-Seite**: Dort zählt nur genau die gewählte Gruppe (ohne Untergruppen); Dienste ohne
  Gruppenzuordnung bleiben immer sichtbar; die Sicht "Bestellungen" (ohne Versiegelt-Häkchen) ignoriert den
  Gruppenfilter ganz.
- **Gruppen-Seite**: Die Baumansicht zeigt nur heute gültige Gruppen; einstige und künftige Gruppen findest du
  in der Tabellenansicht über den Gültigkeits-Filter.
- **Gelöschte Adressen**: Der Filter zeigt ausschliesslich die gelöschten Einträge, nicht zusätzlich zu den
  aktiven.

<!-- level:pitfalls -->

## Stufe 4 — Grenzfälle

- **Ohne angehakte Wurzelgruppe sieht ein Nicht-Admin fast niemanden.** Sobald es eine Gruppe gibt, sieht er in
  den Personenlisten nur Personen ohne jede Gruppe, und das Dashboard zeigt ihm keine Daten.
- **"In keiner Gruppe" versteckt niemanden.** Gruppenlose Personen sind für alle eingeschränkten Benutzer
  sichtbar; auch eine abgelaufene Mitgliedschaft zählt als Gruppe.
- **Gruppe gelöscht**: Das Löschen wird abgelehnt, solange in einem Dienst der Gruppe oder ihrer Untergruppen
  noch Einsätze stehen (egal an welchem Datum; Szenario-Einträge zählen nicht). Gelingt es, verschwinden mit
  den Gruppen auch die Mitgliedschaften aller Personen und die Zuordnungen aller Dienste dieser Gruppen.
  Personen ohne weitere Gruppe sind danach gruppenlos und damit für alle Benutzer mit eingeschränkter
  Gruppen-Sicht sichtbar; in der Gruppenauswahl erscheinen sie unter (de: "Ohne Gruppe", en: "Without group",
  fr: "Sans groupe", it: "Senza gruppo"). Dienste ohne weitere Gruppe sind keiner Gruppe mehr zugeordnet.
  Mitgliedschaften in anderen Gruppen bleiben unverändert.
- **Neu erfasst, aber nicht da**: Eine neue Person mit Eintritt in der Zukunft steht nach dem Speichern nicht in
  der Standardliste, sondern unter (de: "Zukünftige", en: "Future", fr: "Futurs", it: "Futuri").
- **Austritt mit geplanten Einträgen danach**: Einträge, die nach einem neu gesetzten Austrittsdatum schon
  geplant sind, bleiben bestehen; Klacks löscht und markiert sie nicht und warnt beim Speichern nicht. In der
  Zeile sind diese Tage gesperrt; in einer Periode ganz nach dem Austritt wird die Zeile nicht mehr gelistet,
  die Einträge existieren aber weiter.
