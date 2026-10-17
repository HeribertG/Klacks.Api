---
name: explain_terms_with_several_meanings
description: |
  Explains Klacks words whose meaning depends on where they appear: "Dienst" (order, sealed order, plannable
  shift, cut piece, container), "versiegelt" (sealed order, sealed day, lock level Closed, entry of another
  group), the lock levels, "Adresse", "Absenz" (wish, booked absence, absence kind), "Schicht" vs "Dienst",
  "Zurücksetzen", "Gültigkeit", "Mitgliedschaft" and the day a shift past midnight counts for.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - begriff
    - mehrdeutig
    - was meint
    - bedeutet hier
  en:
    - terminology
    - ambiguous term
    - what is meant by
    - meaning here
synonyms:
  de: [was ist der unterschied zwischen bestellung und dienst, heisst versiegelt hier dasselbe wie geschlossen, ist eine adresse eine person oder eine anschrift, was ist ein absenz wunsch im vergleich zur gebuchten absenz, sind schicht und dienst dasselbe, welcher tag zählt bei einem nachtdienst über mitternacht, was ist mit mitgliedschaft gemeint, ich verwechsle die begriffe]
  en: [what is the difference between an order and a shift, does sealed mean the same as closed here, is an address a person or a postal address, absence wish versus booked absence, are schicht and dienst the same thing, which day does a night shift past midnight count for, what does membership mean here, i keep mixing up these terms]
  fr: [quelle différence entre commande et service, scellé veut-il dire clôturé ici, une adresse est-elle une personne, souhait d'absence ou absence réservée, à quel jour compte un service de nuit après minuit, que signifie adhésion ici, je confonds ces termes]
  it: [qual è la differenza tra ordine e turno, sigillato significa chiuso qui, un indirizzo è una persona, desiderio di assenza o assenza prenotata, a quale giorno conta un turno di notte dopo mezzanotte, cosa significa appartenenza qui, confondo questi termini]
---

# Begriffe mit mehreren Bedeutungen

<!-- level:short -->

## Stufe 1 — Kurz gesagt

Einige Wörter tauchen in Klacks an verschiedenen Stellen mit verschiedener Bedeutung auf. "Dienst" kann eine
Bestellung eines Kunden sein, der planbare Dienst im Einsatzplan oder ein zugeschnittenes Teilstück davon.
"Versiegelt" heisst je nach Stelle: Auftrag festgeschrieben, Tag im Periodenabschluss gesperrt, Eintrag geschlossen
oder Eintrag einer anderen Gruppe. Die Seite "Alle Adressen" listet Personen, aber "Neue Adresse erstellen" legt nur
eine neue Anschrift derselben Person an. Und eine Absenz kann ein blosser Wunsch sein oder eine verbindliche Buchung.
Nenn mir das Wort und wo du es gesehen hast, dann sage ich dir, was dort gemeint ist.

<!-- level:elements -->

## Stufe 2 — Die Begriffe im Einzelnen

| Begriff | Bedeutungen | Woran unterscheidbar | Details |
|---|---|---|---|
| **Dienst** | 1. **Bestellung**: Auftrag in Arbeit, frei änderbar, noch nicht planbar. 2. **Versiegelte Bestellung**: festgeschriebener Auftrag. 3. **Planbarer Dienst**: entsteht beim Versiegeln, wird im Einsatzplan gebucht. 4. **Teilstück**: zugeschnittener Teil des planbaren Dienstes. 5. **Container**: bündelt mehrere Aufgaben zu einem Tagespaket. Dazu Dienste ohne Kunde (z. B. Putzen), die sofort als versiegelte Bestellung entstehen, nie als offene Bestellung | Sichten der Dienste-Seite: (de: "Bestellungen", en: "Orders", fr: "Commandes", it: "Ordini"), (de: "Planbare Dienste", en: "Plannable Services", fr: "Services planifiables", it: "Servizi Pianificabili"), (de/en: "Container", fr: "Conteneur", it: "Contenitore") | `explain_shift_lifecycle_order_to_shift` |
| **versiegelt** | 1. Bestellung versiegeln = Stufenwechsel des Auftrags (endgültig). 2. **Tagessiegel** aus dem Periodenabschluss. 3. Sperrstufe (de: "Geschlossen", en: "Closed", fr: "Clôturé", it: "Chiuso") eines Eintrags (rotes Schloss). 4. Eintrag aus einer **fremden Gruppe** im Plan (gruppengesperrt, schreibgeschützt) | Dienste-Maske gesperrt / Tagesspalte gesperrt / rotes Schloss in der Zelle / Tooltip (de: "Gruppeneinschränkung", en: "Group restricted", fr: "Restriction de groupe", it: "Restrizione di gruppo") | `explain_page_period_closing`, `explain_group_model` |
| **Sperrstufe** | Ohne Sperre → (de: "Bestätigt", en: "Confirmed", fr: "Confirmé", it: "Confermato"; grüner Haken) → (de: "Freigegeben", en: "Approved", fr: "Approuvé", it: "Approvato"; gelbes Prüf-Symbol) → (de: "Geschlossen", en: "Closed", fr: "Clôturé", it: "Chiuso"; rotes Schloss). Aufheben: Bestätigt jeder Planer, Freigegeben nur Berechtigte und Admins, Geschlossen nur Admins | Symbol und Tooltip in der Zelle; ohne Sperre kein Symbol | `explain_page_schedule` |
| **Adresse** | 1. Eine **Person** in (de: "Alle Adressen", en: "All Addresses", fr: "Toutes les adresses", it: "Tutti gli indirizzi"); (de: "+ Adresse erfassen", en: "+ Add Address", fr: "Ajouter une adresse", it: "Aggiungi indirizzo") legt eine neue Person an. 2. Eine **Adress-Version** derselben Person: (de: "Neue Adresse erstellen", en: "Create new address", fr: "Créer une nouvelle adresse", it: "Crea un nuovo indirizzo") für einen Umzug. 3. **Adressart**: Haupt-, Geschäfts- oder Rechnungsadresse | Liste vs. Optionsmenü der ersten Card in der Personenmaske | `explain_address_management` |
| **Absenz** | 1. **Wunsch / vorgemerkte Absenz** aus dem Absenzen-Kalender: sperrt nichts. 2. **Gebuchte Absenz** im Einsatzplan: verbindlich, zählt zu den Stunden. 3. **Absenz-Art** (Stammdaten, z. B. Ferien) | Balken über den Zellen vs. Eintrag in der Zelle; im Kalender (de: "Gebucht", en: "Booked", fr: "Réservé", it: "Prenotato") | `explain_absence_model` |
| **Schicht / Dienst** | Beide Wörter stehen in der Oberfläche: Tab (de: "Schichten", en: "Shifts", fr: "Services", it: "Turni") im Einsatzplan, Listentitel (de: "Dienste", en: "Services", fr: "Services", it: "Servizi") auf der Dienste-Seite, Untermenü (de: "Dienste...", en: "Shifts...", fr: "Services...", it: "Turni...") im Kontextmenü einer leeren Zelle | Ort des Labels | `explain_page_schedule`, `explain_page_shifts` |
| **Zurücksetzen** | 1. Im Zuschnitt: offene Teilstücke schliessen und ab einem Datum neu ungeteilt beginnen. 2. In der unteren Leiste einer Bearbeitungsmaske, z. B. der Personenmaske (erscheint nur bei ungespeicherten Änderungen): alle offenen Änderungen verwerfen | Seite, auf der der Button steht: (de: "Zurücksetzen", en: "Reset", fr: "Réinitialiser") | `explain_page_shifts`, `explain_address_management` |
| **Gültigkeit** | Personenliste: Status der Mitgliedschaft relativ zu heute (Aktive / Ehemalige / Zukünftige). Gruppenliste: Gültigkeitszeitraum der Gruppe. Dienste-Liste: Gültigkeitszeitraum des Dienstes | Filter (de: "Gültigkeit", en: "Validity", fr: "Validité", it: "Validità") auf der jeweiligen Seite | `explain_page_employees`, `explain_page_groups`, `explain_page_shifts` |
| **Mitgliedschaft** | 1. **Eintritt/Austritt** der Person (eine einzige Zeile pro Person). 2. **Gruppen-Mitgliedschaft** mit eigenem (de: "GÜLTIG VON", en: "VALID FROM", fr: "VALIDE DÈS", it: "VALIDO DA") / (de: "GÜLTIG BIS", en: "VALID UNTIL", fr: "VALIDE JUSQU'À", it: "VALIDO FINO A") | Card (de/en: "Registration", fr: "Inscription", it: "Registrazione") vs. Card (de: "Zugewiesene Gruppen", en: "Assigned Groups", fr: "Groupes assignés", it: "Gruppi assegnati") | `explain_membership_single_row`, `explain_group_membership_validity` |

<!-- level:effects -->

## Stufe 3 — Warum die Unterschiede zählen

- **Löschen hängt an der Dienst-Stufe**: Eine Bestellung hat einen Papierkorb; planbare Dienste und Teilstücke sind
  nie direkt löschbar; eine versiegelte Bestellung löscht nur ein Admin über Klacksy oder die API, samt planbarem
  Dienst und Teilstücken, und nur solange in dieser Familie nichts verplant ist.
- **Die drei Sperren wirken verschieden**: Das Tagessiegel lehnt jede Neuanlage, Änderung und Löschung am Tag ab
  (für alle oder nur für die Mitglieder der versiegelten Gruppe); die Sperrstufe schützt einen einzelnen Eintrag;
  die Gruppeneinschränkung gilt nur in der Ansicht einer anderen Gruppe.
- **Wunsch und Buchung**: Erst (de: "Absenz übernehmen", en: "Adopt absence", fr: "Reprendre l'absence", it:
  "Riprendere l'assenza") macht aus einem Wunsch gebuchte Absenz-Einträge, Tag für Tag.
- **Austritt und Gruppenfenster**: Die Skills für Ein- und Austritt ändern die Daten der Person, nicht das Fenster
  einer Gruppen-Mitgliedschaft.

<!-- level:pitfalls -->

## Stufe 4 — Grenzfälle

- **Dienst über Mitternacht**: Der Eintrag gehört ganz zu seinem Starttag — mit Stunden und Zuschlägen in der
  Periode des Starttags, im Periodenabschluss und im Lohnexport. Die Standard-Zuschlagsmakros teilen an Mitternacht:
  den Teil nach Mitternacht rechnen sie mit Wochentag und Feiertag des Folgetags.
- **Ausnahme im Plan**: Korrekturen nach Dienstende und der Teil einer Ablösung oder Reise nach Mitternacht erscheinen
  bei einem Nachtdienst am Folgetag und sind dort an dessen Tagessiegel gebunden; in den Periodenstunden zählen sie
  trotzdem zum Starttag des Dienstes.
- **Das Absenz-Untermenü heisst anders**: Im Kontextmenü einer leeren Zelle stehen die Absenz-Arten unter
  (de: "Beschäftigungen...", en: "Absences...", fr: "Absences...", it: "Assenze...").
- **Typ in der Mitgliedschafts-Card**: Das Feld (de: "Typ", en: "Type", fr: "Type", it: "Tipo") neben den Daten
  ändert die Einordnung der Person (Mitarbeiter, Externer, Kunde), nicht die Mitgliedschaft.
- **"Versiegeln" als Bestellung ist endgültig**, ein Tagessiegel nicht: die Periode kann ein Admin wieder öffnen.
