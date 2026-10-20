---
name: explain_why_cannot_edit
description: |
  Explains why a field, entry or mask is greyed out or read-only: sealed orders and plannable shifts with
  the cards that stay editable, sealed days, the lock levels confirmed, approved and closed and who lifts
  them, group-restricted entries, shift editing blocked while a scenario is active, a container template
  opened elsewhere, booked absences, deleted persons, and the rights of admin, supervisor and accounts
  without a role.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - ausgegraut
    - schreibgeschützt
    - gruppeneinschränkung
    - feld gesperrt
  en:
    - greyed out
    - read-only
    - group restricted
    - field locked
synonyms:
  de: [warum ist das feld grau und ich kann nichts eintippen, die maske ist nur zum ansehen offen, warum steht bei dem eintrag gruppeneinschränkung, wieso lässt sich die versiegelte bestellung nicht mehr ändern, der eintrag hat ein rotes schloss, ich sehe nur ein auge statt dem stift, warum kann ich den dienst im szenario nicht zuschneiden, die vorlage ist von jemand anderem gesperrt]
  en: [why is this field grey and i can't type, the mask only opens for viewing, why does the entry say group restricted, why can't i change the sealed order any more, the entry has a red padlock, i only get an eye icon instead of the pencil, why can't i cut the shift inside a scenario, the template is locked by another user]
  fr: [pourquoi ce champ est grisé, le masque s'ouvre seulement en lecture, pourquoi l'entrée indique restriction de groupe, pourquoi la commande scellée n'est plus modifiable, l'entrée a un cadenas rouge, je vois seulement un œil au lieu du crayon, le modèle est bloqué par un autre utilisateur]
  it: [perché questo campo è grigio, la maschera si apre solo in lettura, perché la voce dice restrizione di gruppo, perché l'ordine sigillato non si può più cambiare, la voce ha un lucchetto rosso, vedo solo un occhio invece della matita, il modello è bloccato da un altro utente]
---

# Warum kann ich das nicht bearbeiten?

<!-- level:short -->

## Stufe 1 — Kurz gesagt

Ausgegraut heisst in Klacks fast immer "bewusst geschützt", nicht "kaputt". Geschützt werden zum Beispiel
festgeschriebene Aufträge (versiegelte Bestellungen), gesperrte Tage, Einträge mit einer Sperrstufe und Buchungen,
die einer anderen Gruppe gehören. Manchmal fehlt auch nur das Recht: Ein Konto ohne Bearbeitungsrecht sieht statt
des Stifts ein Auge und darf Stammdaten nur ansehen. Bei den meisten Sperren gibt es einen Weg zurück, bei einigen
nur über einen Admin. Sag mir, was du ändern willst, dann finden wir den Grund.

<!-- level:elements -->

## Stufe 2 — Gründe, Erkennen, Abhilfe

| Grund | Woran erkennbar | Abhilfe | Details |
|---|---|---|---|
| **Versiegelte Bestellung** | Geschlossenes Schloss in der Maske, die meisten Felder gesperrt | Änderbar bleiben die Karten (de: "Zugeordnete Gruppen", en: "Assigned Groups", fr: "Groupes attribués", it: "Gruppi assegnati"), (de: "Erforderliche Qualifikationen", en: "Required Qualifications", fr: "Qualifications requises", it: "Qualifiche richieste"), (de: "Standard-Spesen", en: "Default Expenses", fr: "Frais par défaut", it: "Spese predefinite") und (de/en/fr/it: "Macro") sowie ein noch leeres Bis-Datum (einmalig). Löschen kann nur ein Admin über Klacksy (die Liste zeigt keinen Papierkorb), und nur solange auf der Bestellung nichts verplant ist und keine Szenario-Kopie oder Container-Verwendung besteht | `explain_shift_lifecycle_order_to_shift` |
| **Planbare Schicht / Teilstück** | Kein Papierkorb, Felder gesperrt wie bei der Bestellung | Nie direkt löschbar; eine Schnittgruppe endet über (de: "Zurücksetzen", en: "Reset", fr: "Réinitialiser", it: "Ripristina") im Zuschnitt | `explain_page_shifts` |
| **Tag gesperrt** | Spaltenkopf als gesperrt markiert, leere Zellen ohne Kontextmenü; Änderungen an dem Tag werden abgelehnt | Freigegebener Tag: Supervisor oder Admin nimmt die Freigabe über Klacksy zurück. Abgeschlossene Periode: Admin öffnet sie mit Begründung wieder | `explain_page_period_closing` |
| **Dunkle oder blaue Zelle im Einsatzplan** | Die Zelle ist abgedunkelt (dunkel) oder blau abgedunkelt (dunkelblau), das Kontextmenü bleibt leer | Dunkel: die Person ist an dem Tag nicht angestellt, Eintritts- und Austrittsdatum prüfen. Blau: der Tag liegt ausserhalb der Gruppenmitgliedschaft, deren Beginn oder Ende anpassen | `explain_why_cannot_book` |
| **Eintrag (de: "Bestätigt", en: "Confirmed", fr: "Confirmé", it: "Confermato")** (grüner Haken) | Kontextmenü bietet nur noch (de: "Bestätigung aufheben", en: "Revoke confirmation", fr: "Annuler la confirmation", it: "Annullare conferma") und (de: "Im Schichtplan anzeigen", en: "Show in shift schedule", fr: "Afficher dans le planning", it: "Mostra nel piano turni") | Aufheben darf jeder, der planen darf | `explain_page_schedule` |
| **Eintrag (de: "Freigegeben", en: "Approved", fr: "Approuvé", it: "Approvato")** (gelbes Prüf-Symbol) | wie oben; ohne Recht fehlt der Punkt zum Aufheben | Aufheben nur Supervisor oder Admin | `explain_page_schedule` |
| **Eintrag (de: "Geschlossen", en: "Closed", fr: "Clôturé", it: "Chiuso")** (rotes Schloss) | wie oben; ohne Recht fehlt der Punkt zum Aufheben | Aufheben nur Admin; liegt die Periode abgeschlossen darunter, muss ein Admin sie wieder öffnen | `explain_page_period_closing` |
| **Fremde Gruppe** | Tooltip (de: "Gruppeneinschränkung", en: "Group restricted", fr: "Restriction de groupe", it: "Restrizione di gruppo"), Kontextmenü leer | In der Ansicht der Gruppe ändern, der der Dienst gehört | `explain_group_model` |
| **Szenario aktiv** | Zuschnitt-Buttons gesperrt, Speichern im Dienst-Formular und im Zuschnitt gesperrt (Tooltip nennt das Szenario) | Szenario übernehmen oder verwerfen | `explain_page_schedule` |
| **Container-Vorlage anderswo geöffnet** | Hinweis-Banner, Seite schreibgeschützt; bei einer zweiten eigenen Sitzung (de: "Container ist schon geöffnet und deswegen gesperrt.", en: "Container is already open and therefore locked.", fr: "Le conteneur est déjà ouvert et donc verrouillé.", it: "Il contenitore è già aperto e quindi bloccato.") | Andere Sitzung schliessen | `explain_shift_container` |
| **Gebuchte Absenz im Absenzen-Kalender** | Felder deaktiviert, (de: "Gebucht", en: "Booked", fr: "Réservé", it: "Prenotato") in der Notiz-Spalte | Im Schichtplan ändern oder löschen | `explain_page_absence` |
| **Gelöschte Person** | Nur mit dem Filter (de: "Gelöschte Adressen", en: "Deleted Addresses", fr: "Adresses supprimées", it: "Indirizzi Eliminati") sichtbar, Karten gesperrt | Keine — nicht wiederherstellbar | `explain_page_employees` |
| **Fehlendes Recht** | Auge statt Stift, Erfassen-Buttons fehlen | Admin vergibt die Stufe in der Benutzerverwaltung | `explain_roles_and_permissions` |

<!-- level:effects -->

## Stufe 3 — Zusammenspiel

- **Rechte in kurz**: Admin darf alles. Supervisor darf Personen, Gruppen, Verträge und Dienste anlegen und
  bearbeiten, aber nicht löschen; er darf Tage freigeben und Freigaben zurücknehmen (über Klacksy). Ein Konto ohne
  Rolle liest, plant (Einträge setzen, ändern, entfernen, Absenzen erfassen) und schreibt Notizen, ändert aber keine
  Stammdaten. Einstellungen, Benutzerverwaltung und Periodenabschluss sind Admin-only.
- **Ohne Bearbeitungsrecht für Dienste** ist auf der Dienste-Seite nur die Sicht (de: "Planbare Dienste",
  en: "Plannable Services", fr: "Services planifiables", it: "Servizi Pianificabili") wählbar.
- **Die Gruppeneinschränkung gilt nur bei gewählter Gruppe.** Welcher Gruppe eine Buchung gehört, bestimmt die
  Gruppe ihres Dienstes, nicht die der Person; Dienste ganz ohne Gruppe werden so nie gesperrt.
- **Sperren und Neuberechnung**: Bestätigte, freigegebene und geschlossene Einträge verändert keine der beiden
  Stunden-Neuberechnungen.
- **Auch Admins bearbeiten gesperrte Einträge nicht direkt.** Im Schichtplan und über Klacksy ist jeder Eintrag mit
  Sperrstufe für alle schreibgeschützt; erst die Sperre aufheben, dann ändern. Geschlossene Einträge und gesperrte
  Tage lehnt das System für jeden ab, auch für Admins.

<!-- level:pitfalls -->

## Stufe 4 — Grenzfälle

- **Versiegeln ist erst nach dem Speichern endgültig.** Der Klick auf den Schloss-Button ändert zunächst nur das
  offene Formular; wer es verwirft, behält die Bestellung.
- **Kunde und Dienst-Typ** sind nach dem Versiegeln fest — ein kundenloser Dienst bekommt nachträglich keinen
  Kunden.
- **Das leere Bis-Datum geht nur einmal.** Danach ist es gesperrt wie die anderen Felder. Über Klacksy wird
  zusätzlich geprüft, dass nach dem Datum nichts verplant ist.
- **Bestätigen geht nur aus dem ungesperrten Zustand.** Ein freigegebener oder geschlossener Eintrag lässt sich
  nicht zurück auf bestätigt setzen.
- **Aufheben setzt immer ganz auf offen.** Wer bei einem freigegebenen oder geschlossenen Eintrag die Sperre
  aufhebt, landet nicht bei bestätigt, sondern ohne Sperrstufe.
- **Wiederöffnen einer Periode stellt die vorherige Stufe her**: bestätigt bleibt bestätigt, freigegeben bleibt
  freigegeben. Nur Einträge, die vor Einführung dieser Speicherung abgeschlossen wurden, kommen offen zurück.
- **Das eigene Konto** kann sich in der Benutzerverwaltung weder selbst bearbeiten noch die eigenen Rechte ändern.
