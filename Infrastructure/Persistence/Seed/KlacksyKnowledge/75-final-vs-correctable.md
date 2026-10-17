---
name: explain_final_vs_correctable
description: |
  Explains which actions in Klacks can be taken back and which are final, as there is no general step-back
  button: sealing an order, accepting or discarding a scenario, closing and reopening a period, deleting a
  person, deleting and restoring work entries, deleting a group (refused while works are planned), setting
  an exit date, changing the person type, resetting a shift cut, adopting an absence wish and Klacksy
  counter-actions.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - umkehrbar
    - endgültig
    - wiederherstellbar
    - gegenweg
  en:
    - reversible
    - permanent
    - restorable
    - take back
synonyms:
  de: [kann ich das wieder zurücknehmen, ist das löschen endgültig, gibt es einen weg zurück nach dem versiegeln, wie hole ich einen gelöschten einsatz zurück, kann man eine gelöschte gruppe wiederherstellen, was ist nach dem übernehmen des szenarios weg, lässt sich der periodenabschluss aufheben, was passiert wenn ich mich vertan habe]
  en: [can i take that back, is deleting permanent, is there a way back after sealing, how do i get a deleted work entry back, can a deleted group be restored, what is lost after accepting the scenario, is closing a period final, what if i made a mistake]
  fr: [puis-je revenir en arrière, la suppression est-elle définitive, y a-t-il un retour après le scellement, comment récupérer une affectation supprimée, peut-on restaurer un groupe supprimé, la clôture de période est-elle réversible, que faire si je me suis trompé]
  it: [posso tornare indietro, l'eliminazione è definitiva, c'è un ritorno dopo la sigillatura, come recupero un'assegnazione eliminata, si può ripristinare un gruppo eliminato, la chiusura del periodo è reversibile, cosa faccio se ho sbagliato]
---

# Endgültig oder korrigierbar?

<!-- level:short -->

## Stufe 1 — Kurz gesagt

Klacks kennt **kein generelles Rückgängigmachen** — es gibt keinen Knopf, der beliebige letzte Schritte zurücknimmt.
Für die meisten Aktionen gibt es stattdessen einen **Gegenweg**: eine Bestätigung aufheben, eine Periode wieder
öffnen, ein Austrittsdatum wieder leeren, einen gelöschten Einsatz gezielt wiederherstellen. Ein paar Aktionen
sind aber **endgültig**: eine Bestellung versiegeln, eine Person oder eine Gruppe löschen, ein Szenario übernehmen
oder verwerfen. Prüfe vor diesen Schritten genau, was betroffen ist. Frag mich vorher, wenn du unsicher bist —
danach kann ich es oft nicht mehr zurückholen.

<!-- level:elements -->

## Stufe 2 — Aktion für Aktion

| Aktion | Endgültig? | Gegenweg / Ausnahme | Details |
|---|---|---|---|
| **Bestellung versiegeln** | Ja, sobald gespeichert | Bis zum Speichern verwerfbar. Danach nur: ein leeres Bis-Datum einmalig nachtragen; ein Admin löscht die versiegelte Bestellung samt planbarer Schicht und Teilstücken über Klacksy oder die API, solange in der Familie nichts verplant ist (keine Einsätze, keine Szenario-Kopien, keine Container-Verweise) | `explain_shift_lifecycle_order_to_shift` |
| **Stapel-Versiegeln** (über Klacksy) | Ja | Vorschau zuerst; nur für Benutzer mit uneingeschränktem Gruppenumfang; was versiegelt ist, bleibt es | `explain_shift_lifecycle_order_to_shift` |
| **Szenario übernehmen** | Ja | Ersetzt den echten Plan in Zeitraum und Gruppe des Szenarios; Einträge an gesperrten Tagen werden nicht übernommen. Kein Gegenweg — vorher prüfen | `explain_page_schedule` |
| **Szenario verwerfen** | Ja | Löscht die Szenario-Einträge; der echte Plan bleibt unberührt. Ein verworfenes Szenario lässt sich nicht zurückholen | `explain_page_schedule` |
| **Periode abschliessen** | Nein | Ein Admin öffnet sie mit Begründung wieder; jeder Eintrag bekommt die Sperrstufe zurück, die er vor dem Abschluss hatte | `explain_period_closing_model` |
| **Einsatz löschen** | Nein (gezielt) | Wiederherstellen: wer ihn gelöscht hat, in den ersten 60 Sekunden; ein Admin jederzeit. Im Plan erscheint nach dem Löschen kurz die Meldung (de: "Dienst gelöscht", en: "Shift deleted", fr: "Service supprimé", it: "Turno eliminato") mit einer Schaltfläche zum Wiederherstellen | `explain_page_schedule` |
| **Mehrere Einsätze löschen** (über Klacksy) | Nein, aber nur einzeln zurück | Höchstens 50 Einsätze einer Person, nur nach Bestätigung genau der gezeigten Einsätze samt Container-Einträgen. Zurück geht es nur Einsatz für Einsatz — mit denselben Fristen wie beim einzelnen Einsatz | `explain_page_schedule` |
| **Person löschen** | Ja (aus Sicht der App) | Soft-Delete: über den Filter (de: "Gelöschte Adressen", en: "Deleted Addresses", fr: "Adresses supprimées", it: "Indirizzi Eliminati") weiter einsehbar, aber nicht wiederherstellbar. Nur Admin | `explain_page_employees` |
| **Austrittsdatum setzen** | Nein | Lässt sich im selben Feld ändern oder leeren | `explain_address_management` |
| **Personentyp ändern** | Nein | Das Speichern scheitert, solange eine Adressart nicht zum neuen Typ passt; eine juristische Person darf nur Kunde sein | `explain_address_management` |
| **Gruppe löschen** | Ja | Löscht die Gruppe mit allen Untergruppen, nur Admin, keine Wiederherstellung. Mitgliedschaften und Dienst-Zuordnungen der gelöschten Gruppen werden mit entfernt. Abgelehnt, solange für einen Dienst des Teilbaums Einsätze geplant sind | `explain_page_groups` |
| **Zuschnitt zurücksetzen** | Nein für frühere Schnitte | (de: "Zurücksetzen", en: "Reset") schliesst die offenen Teilstücke vor dem neuen Startdatum und legt ab dort eine ungeteilte planbare Schicht an; frühere Schnitte bleiben | `explain_page_shifts` |
| **Absenz-Wunsch übernehmen** | Ja für den Wunsch | Der Wunsch wird gelöscht; zurück nur, indem man die gebuchten Tage löscht und einen neuen Wunsch erfasst | `explain_absence_model` |
| **Einsatz bestätigen** | Nein | (de: "Bestätigung aufheben", en: "Revoke confirmation") | `explain_page_schedule` |

<!-- level:effects -->

## Stufe 3 — Zusammenspiel

- **Was das System schon verlassen hat, kommt nicht zurück.** Ein Wiederöffnen nimmt erzeugte Exporte nicht
  zurück; sie bleiben im Export-Protokoll. Ein erneuter Lohnexport nach der Korrektur enthält nur neue oder
  geänderte Personen und ist als Nachtrag markiert.
- **Gruppe löschen macht Personen gruppenlos.** Wer danach in keiner Gruppe mehr ist, ist für alle Benutzer mit
  eingeschränkter Gruppen-Sicht sichtbar; ein Dienst ohne weitere Gruppe ist danach keiner Gruppe mehr
  zugeordnet. Mitgliedschaften in anderen Gruppen bleiben unverändert. Vor dem Löschen müssen die geplanten
  Einsätze der Dienste dieser Gruppen bereinigt sein, sonst erscheint (de: "Diese Gruppe kann nicht gelöscht
  werden, da noch Dienste geplant sind.", en: "This group cannot be deleted because planned work entries exist.",
  fr: "Ce groupe ne peut pas être supprimé car des entrées de travail planifiées existent.", it: "Questo gruppo
  non può essere eliminato perché esistono voci di lavoro pianificate.").
- **Austritt statt Löschen**: Verlässt jemand nur das Unternehmen, ist das Austrittsdatum der richtige Weg, weil
  es korrigierbar ist. Ab dem Tag nach dem Austritt lässt sich nicht mehr planen. Bereits geplante Einträge nach
  einem neu gesetzten Austrittsdatum bleiben stehen — ohne Warnung; im Plan sind diese Tage gesperrt.
- **Klacksy nimmt nichts selbst zurück**: Klacksy kann deine eigene letzte erfolgreiche Klacksy-Änderung aus den
  letzten 30 Minuten nennen und die Gegenaktion vorschlagen; entscheiden und ausführen musst du. Manche Aktionen,
  etwa das Anlegen einer Person oder das Übernehmen eines Szenarios, haben kein automatisches Gegenstück und
  werden von Hand korrigiert.
- **Szenario übernehmen trifft auch Regeln**: Würde das Übernehmen Verstösse gegen eine Arbeitszeit-Regel auf
  "Blockieren" erzeugen, wird es als Ganzes abgelehnt. Übersteuern kann nur ein Admin oder Supervisor, wenn die
  Firmeneinstellung das erlaubt und ausdrücklich übersteuert wird; jedes Übersteuern wird protokolliert.

<!-- level:pitfalls -->

## Stufe 4 — Grenzfälle

- **Wiederöffnen bei Altfällen**: Einträge, die abgeschlossen wurden, bevor Klacks die vorherige Sperrstufe
  speicherte, kommen offen zurück; eine frühere Bestätigung oder Freigabe ist bei ihnen verloren.
- **Wiederöffnen pro Gruppe** hebt nur die Sperren dieser Gruppe auf; bei Absenzen zusätzlich die, die keiner
  abschliessenden Gruppe zugeordnet sind.
- **Szenario mit veraltetem Stand**: Hat sich der echte Plan seit dem Erstellen des Szenarios geändert, würde
  das Übernehmen diese Änderungen überschreiben; Klacks verlangt dafür zuerst eine ausdrückliche Bestätigung.
- **Einsatz wiederherstellen kann scheitern**: Das Wiederherstellen prüft wie ein neues Anlegen — Tagessperre,
  Kapazität beim sporadischen Dienst und harte Konflikte. Ist der Platz inzwischen belegt, erscheint (de:
  "Wiederherstellen nicht möglich, der Platz ist inzwischen belegt", en: "Restore not possible, the slot is now
  taken"). Einen versiegelten Einsatz kann nur ein Admin wiederherstellen.
- **Mehrere Einsätze löschen**: Ändert sich bis zur Bestätigung etwas, wird nichts gelöscht und Klacksy zeigt
  eine neue Vorschau; gesperrte Einträge oder versiegelte Tage verhindern den ganzen Aufruf.
- **Gruppe löschen zählt alle Einsätze**: Geprüft werden echte Einsätze an jedem Datum, auch vergangene;
  Szenario-Einträge zählen nicht.
- **Frühere Mitgliedschaftsdaten sind weg**: Es gibt pro Person nur eine Mitgliedschaft; ein Wiedereintritt
  überschreibt das alte Eintrittsdatum, nichts archiviert die frühere Zeit.
- **Juristische Person**: Haken setzen und wieder entfernen merkt sich den alten Typ nicht — ein früherer Externer
  wird danach Mitarbeiter.
- **Gelöschte Arbeitszeit-Regeln aus einem Regional-Setup** kommen beim nächsten Import desselben Setups wieder;
  von Hand angelegte Regeln rührt ein Import nie an.
