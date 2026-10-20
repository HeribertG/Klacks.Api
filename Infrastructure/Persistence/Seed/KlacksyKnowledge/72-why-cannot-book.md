---
name: explain_why_cannot_book
description: |
  Explains why a shift cannot be placed for a person on a day: empty right-click menu, shift not offered, or
  save refused. Covers sealed or approved days, days before entry or after exit, group membership windows,
  used-up sporadic shifts, fully staffed shifts, public holidays, a missing mandatory qualification or one
  below a binding level (never overridable), working-time rules set to block (stop Klacksy and assistants,
  not direct entries), and scenario bookings.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - kann nicht buchen
    - buchung abgelehnt
    - kontextmenü leer
    - dienst lässt sich nicht eintragen
  en:
    - cannot book
    - booking refused
    - empty context menu
    - cannot place a shift
synonyms:
  de: [warum lässt sich hier kein dienst eintragen, wieso ist das kontextmenü in dieser zelle leer, der dienst wird mir für diesen tag nicht angeboten, warum kann ich den mitarbeiter nach seinem austritt nicht mehr einteilen, warum sind die tage vor seinem eintritt grau, wieso bekomme ich den sporadischen dienst nicht mehr gebucht, warum wird die buchung wegen fehlender qualifikation abgelehnt]
  en: [why can't i put a shift in this cell, the right-click menu on this day is empty, the shift is not offered for that date, why can't i schedule him after his last working day, why are the days before her start date locked, the sporadic shift cannot be booked any more, why is the booking refused because of a missing qualification]
  fr: [pourquoi je n'arrive pas à placer un service dans cette case, le menu contextuel de ce jour est vide, le service n'est pas proposé pour cette date, pourquoi je ne peux plus planifier l'employé après son départ, le service sporadique n'est plus réservable, pourquoi la réservation est refusée faute de qualification]
  it: [perché non riesco a mettere un turno in questa cella, il menu contestuale di questo giorno è vuoto, il turno non viene proposto per quella data, perché non posso più pianificarlo dopo l'uscita, il turno sporadico non si può più prenotare, perché la prenotazione viene rifiutata per una qualifica mancante]
---

# Warum kann ich hier nicht buchen?

<!-- level:short -->

## Stufe 1 — Kurz gesagt

Wenn sich ein Dienst nicht eintragen lässt, hat Klacks fast immer einen festen Grund: Der Tag ist
versiegelt oder freigegeben, die Person ist an diesem Tag noch nicht oder nicht mehr dabei, der Dienst
ist an diesem Tag nicht buchbar, oder der Person fehlt eine Pflicht-Qualifikation. Auf geschlossenen Tagen,
vor dem Eintrittsdatum und nach dem Austrittsdatum bleibt das Kontextmenü einer leeren Zelle einfach leer.
Ein vorgemerkter Absenz-Wunsch ist dagegen nie der Grund — er sperrt nichts. Wer in einem Szenario
arbeitet, bucht ins Szenario: die Buchung erscheint dann nicht im Original-Plan. Für eine bestimmte Person,
einen Tag und einen Dienst kann Klacksy die Zelle direkt prüfen und den zutreffenden Grund nennen.

<!-- level:elements -->

## Stufe 2 — Gründe, Erkennen, Abhilfe

| Grund | Woran erkennbar | Abhilfe | Details |
|---|---|---|---|
| **Tag versiegelt** (Periode geschlossen) oder **Tag freigegeben** | Sind alle Dienst-Einträge des Tages geschlossen, bleibt das Kontextmenü leerer Zellen leer. Sonst — etwa bei einer Tagesfreigabe, einem Gruppensiegel oder einem versiegelten Tag ohne Einträge — öffnet sich das Menü, aber das Speichern im echten Plan wird abgelehnt | Eine geschlossene Periode kann nur ein Admin wieder öffnen (auf der Seite Periodenabschluss, mit Begründung); eine Tagesfreigabe heben Admins oder Berechtigte der freigebenden Gruppe auf | `explain_page_period_closing` |
| **Vor dem Eintrittsdatum** | Die Zellen sind dunkel abgedunkelt, das Kontextmenü leerer Zellen bleibt leer, keine neuen Einträge möglich | Eintrittsdatum (Beginn der Mitgliedschaft) prüfen | `explain_address_management` |
| **Nach dem Austrittsdatum** | Das Austrittsdatum ist der letzte Arbeitstag; ab dem Folgetag sind die Zellen dunkel abgedunkelt, das Kontextmenü bleibt leer und die Planung wird abgelehnt; in einer Periode ganz nach dem Austritt steht die Zeile gar nicht mehr im Plan | Austrittsdatum ändern oder leeren — es ist eine Korrektur, kein Einbahnschritt | `explain_address_management` |
| **Ausserhalb der Gruppen-Mitgliedschaft** | Bei gewählter Gruppe sind die Tage ausserhalb (de: "GÜLTIG VON", en: "VALID FROM", fr: "VALIDE DÈS", it: "VALIDO DA") / (de: "GÜLTIG BIS", en: "VALID UNTIL", fr: "VALIDE JUSQU'À", it: "VALIDO FINO A") in der Zeile gesperrt und blau abgedunkelt (im Alltag dunkelblau genannt) — auch bei einer Person, die schon lange angestellt ist; wie viele Personen einer Gruppe betroffen sind, zeigt `diagnose_planning_period` | Daten der Mitgliedschaft in der Card (de: "Zugewiesene Gruppen", en: "Assigned Groups", fr: "Groupes assignés", it: "Gruppi assegnati") anpassen | `explain_group_membership_validity` |
| **Sporadischer Dienst ausgeschöpft** | Leere Tage sind abgeblendet und nicht ziehbar, sobald die Periode ihre erlaubten Tage verbraucht hat; eine Einzelbuchung lehnt der Server ab | Andere Periode oder anderer Dienst; die Grenzen sind nach dem Versiegeln fest | `explain_shift_sporadic` |
| **Dienst voll besetzt** | Das Untermenü (de: "Dienste...", en: "Shifts...", fr: "Services...", it: "Turni...") zeigt nur Dienste des Tages mit freier Kapazität | Anderer Dienst oder Kapazität des Dienstes prüfen | `explain_page_schedule` |
| **Feiertag, für den der Dienst nicht gilt** | Am Feiertag wird der Dienst nicht angeboten, obwohl der Wochentag angehakt ist (ohne Feiertagsoption), oder der Feiertag fällt auf einen nicht angehakten Tag | Beim Dienst (de: "Zusätzlich an jedem Feiertag", en: "Plus every public holiday", fr: "En plus chaque jour férié", it: "In più ogni giorno festivo") oder (de: "Gewählte Tage, auch an Feiertagen", en: "Selected days, holidays included", fr: "Jours choisis, fériés inclus", it: "Giorni scelti, festivi inclusi") setzen — vor dem Versiegeln, danach gesperrt | `explain_shift_holiday_options` |
| **Pflicht-Qualifikation fehlt**, liegt unter einer verbindlichen Mindeststufe oder ist abgelaufen (abgelaufen sperrt nur mit der Firmeneinstellung QUALIFICATION_EXPIRED_MANDATORY_BLOCKS oder bei zusätzlich zu tiefer verbindlicher Stufe, sonst nur Warnung) | Das Speichern wird abgelehnt, z. B. mit (de: "{{client}} fehlt die Pflicht-Qualifikation „{{qualification}}“ für den Dienst {{shift}}.", en: "{{client}} is missing the mandatory qualification “{{qualification}}” for shift {{shift}}.", fr: "{{client}} n’a pas la qualification obligatoire « {{qualification}} » pour le service {{shift}}.", it: "A {{client}} manca la qualifica obbligatoria «{{qualification}}» per il turno {{shift}}.") | Qualifikation mit ausreichender Stufe und gültigem Zeitraum bei der Person erfassen. Nur bei zu tiefer Stufe zusätzlich möglich: die Anforderung beim Dienst auf (de: "Nur Hinweis", en: "Hint only", fr: "Simple remarque", it: "Solo avviso") stellen, wo das vertretbar ist — gegen eine ganz fehlende Qualifikation hilft das nicht | `explain_qualifications` |
| **Arbeitszeit-Regel auf "Blockieren"** | Klacksy und die Planungs-Assistenten lassen die Zuteilung weg (ab Werk wird nur gewarnt); wer direkt im Einsatzplan einträgt, wird nicht gestoppt — der Verstoss erscheint in der Fehlerliste | Plan anpassen; übersteuern nur mit Recht, erlaubender Firmeneinstellung und ausdrücklichem Wunsch | `explain_scheduling_rules_model`, `explain_compliance_rules_model` |
| **Zelle schon belegt** | Liegt in der Zelle schon ein Dienst oder eine Absenz, nimmt sie keinen weiteren Dienst an: beim Ziehen erscheint ein roter Rahmen, beim Loslassen passiert nichts. Ein bestätigter Eintrag ist gesperrt | Den bestehenden Eintrag zuerst entfernen oder verschieben; bei einem bestätigten Eintrag zuerst die Bestätigung aufheben | `explain_page_schedule` |
| **Eintrag aus einer anderen Gruppe** | Ein Eintrag mit einem Dienst einer Gruppe, die du nicht siehst, ist für dich schreibgeschützt: er lässt sich nicht ziehen, sein Kontextmenü bleibt leer | Jemanden fragen, der diese Gruppe sieht, oder einen Admin | `explain_page_schedule` |
| **Dienst ausserhalb deiner Gruppen** | Ein Dienst, der nur Gruppen zugeordnet ist, die du nicht siehst, wird dir nicht angeboten; der Server behandelt ihn wie einen Dienst, den es nicht gibt | Einen Admin bitten, den Dienst einer deiner Gruppen zuzuordnen oder dir die Gruppe freizugeben | `explain_page_schedule` |
| **Dienst ausserhalb seiner Gültigkeit** | Vor dem ersten und nach dem letzten gültigen Tag des Dienstes wird er an diesem Tag nicht angeboten | Gültigkeit des Dienstes prüfen oder den Dienst nehmen, der an diesem Tag gilt | `explain_page_schedule` |
| **Szenario aktiv** | Szenario-Name statt (de/en/fr: "Original") auf dem Szenario-Button | Kein Fehler: die Buchung liegt im Szenario; zum Original wechseln oder das Szenario übernehmen | `explain_page_schedule` |

<!-- level:effects -->

## Stufe 3 — Zusammenspiel

- **Tagessiegel mit und ohne Gruppe**: Ein Siegel ohne Gruppe sperrt den Tag für alle. Ein Siegel mit
  Gruppe sperrt Mitarbeitende, die an diesem Tag aktives Mitglied dieser Gruppe sind oder einen Dienst
  dieser Gruppe gebucht haben. Eine Tagesfreigabe wirkt genauso, als Siegel ihrer Gruppe. Im echten Plan
  lehnt das Siegel jede Änderung an Diensten, Absenzen, Spesen und Korrekturen ab.
- **Mitgliedschaft zählt pro genauer Gruppe**: Ist eine Elterngruppe gewählt, kommen die Sperrdaten nur aus
  Mitgliedschaften in genau dieser Gruppe, nicht aus ihren Untergruppen.
- **Planungs-Wizards**: Beim Übernehmen ihres Ergebnisses werden Platzierungen auf Tagen ausserhalb der
  Mitgliedschaft oder des Gruppenfensters einzeln weggelassen; der Rest wird übernommen.
- **Szenario**: Im Szenario überspringt der Server die Tagessperren-Prüfung (Sandbox); verbindlich wird die
  Sperre erst beim Übernehmen in den echten Plan. Der Einsatzplan selbst sperrt geschlossene Tage aber auch im
  Szenario: Kontextmenü leer, Einfügen überspringt sie.
- **Qualifikation — was sperrt, was nur warnt**: Eine fehlende Pflicht-Qualifikation sperrt immer. Liegt die
  Stufe zu tief, wird nur gewarnt, ausser die Mindeststufe ist verbindlich — an der Qualifikation per
  (de: "Mindeststufe ist verbindlich", en: "Minimum level is binding", fr: "Le niveau minimum est
  contraignant", it: "Il livello minimo è vincolante") oder beim Dienst pro Anforderung mit (de:
  "Verbindlich", en: "Binding", fr: "Contraignant", it: "Vincolante"). Eine abgelaufene Pflicht-Qualifikation
  warnt nur, ausser die Firmeneinstellung QUALIFICATION_EXPIRED_MANDATORY_BLOCKS ist eingeschaltet oder die
  Stufe liegt unter einer verbindlichen Mindeststufe. Eine nicht verpflichtende Qualifikation sperrt nie.
- **Feiertags-Optionen**: Ohne Option fällt der Dienst an Feiertagen auf seinen angehakten Tagen aus.
  "Zusätzlich an jedem Feiertag" (in der Dienstliste (de: "FT", en: "HOL", fr: "JF", it: "FER")) = angehakte
  Tage plus jeder Feiertag, egal welcher Wochentag. "Gewählte Tage, auch an Feiertagen" (in der Dienstliste
  (de: "+FT", en: "+HOL", fr: "+JF", it: "+FER")) = genau die angehakten Tage, auch wenn einer davon ein
  Feiertag ist; Feiertage an nicht angehakten Tagen zählen nicht. Details: `explain_shift_holiday_options`.

<!-- level:pitfalls -->

## Stufe 4 — Grenzfälle

- **Ein Wunsch sperrt nichts.** Vorgemerkte Absenzen aus dem Absenzen-Kalender blockieren den Schichtplan nicht;
  die Person kann an dem Tag gebucht werden. Soll sie geschützt sein, den Wunsch vorher übernehmen
  ((de: "Absenz übernehmen", en: "Adopt absence", fr: "Reprendre l'absence", it: "Riprendere l'assenza")).
- **Abgeblendet heisst nicht immer dasselbe.** Beim sporadischen Dienst sind "gebucht" und "gesperrt" beide
  nicht ziehbar; "gebucht" heisst, der Tag selbst ist voll, "gesperrt", die Periode ist erschöpft.
- **Einfügen mehrerer Zellen prüft weniger.** Die Sammel-Buchung beim Einfügen prüft die sporadischen Grenzen
  nicht — achte selbst auf abgeblendete Tage. Versiegelte Tage und Tage ausserhalb der Mitgliedschaft oder
  des Gruppenfensters überspringt das Einfügen stillschweigend.
- **Qualifikation ist nicht übersteuerbar.** Eine Ablehnung wegen fehlender Pflicht-Qualifikation oder zu
  tiefer verbindlicher Stufe kann niemand wegklicken, auch kein Admin. Ausnahme: Im Szenario wird eine
  direkt im Plan eingetragene Buchung nicht auf Qualifikation geprüft; Klacksy prüft auch dort.
- **Ein gesperrtes Zeitfenster ist immer ein Veto.** Ein gesperrtes Zeitfenster blockiert in der automatischen
  Planung immer, auch wenn es auf "Warnen" steht.
- **Doppelbuchung**: Wer direkt im Einsatzplan einträgt oder verschiebt, wird nicht gestoppt — die
  Doppelbuchung wird gespeichert und erscheint in der Fehlerliste. Klacksy und die Planungs-Assistenten
  lehnen sie ab, und dort lässt sie sich nicht übersteuern.
- **Einsatz an andere Person übergeben**: Gesperrte Einträge, Einträge in Containern, gesperrte Tage und Tage
  ausserhalb der Mitgliedschafts- oder Gruppenperiode der neuen Person werden abgelehnt.
