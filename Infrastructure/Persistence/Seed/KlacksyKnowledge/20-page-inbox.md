---
name: explain_page_inbox
description: |
  Explains the Inbox page (Posteingang) at /workplace/inbox — the three-column mail client
  for the connected IMAP mailbox, visible only when an IMAP account (server, username,
  password) is configured in settings. Covers the Folders column (localized system folders
  Inbox/Spam/Trash/Sent/Clients, user folders with create/delete, the Groups tree with
  unread badges), the email list (Relevant/Other/All tabs, read filter, date sort, refresh
  button, right-click context menu with mark read/unread, spam/not-spam, move to folder,
  delete/restore/permanently delete) and the reading pane (From/To/Date/Folder metadata,
  sanitized HTML body, read toggle and delete actions). Use this when the user asks what
  they see on the Inbox page, what the folders/tabs/badges/context-menu entries mean, or
  how to work with it. Supports a level parameter: short (purpose only), elements (every
  element explained), effects (IMAP mirroring, polling and spam classification, unread
  badges, visibility rules and how the page interacts with the settings page).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - inbox
  - posteingang
  - e-mail
  - email
  - mail
  - ordner
  - spam
  - spamverdacht
  - junk
  - ungelesen
  - unread
  - unread count
  - ungelesene mails
  - papierkorb
  - trash
  - wiederherstellen
  - restore
  - lesefilter
  - read filter
  - relevante mails
  - relevant emails
  - sortieren
  - sort by date
  - anhang
  - attachments
  - ordnerstruktur
  - folder hierarchy
  - kontextmenü
  - rechtsklick
  - imap
  - gesendet
  - sent
synonyms:
  de: [posteingang, was sehe ich hier, erkläre diese seite, was bedeutet dieser ordner, was bedeutet dieses badge, e-mails lesen, mail-liste, spamverdacht, ungelesene mails, wie viele ungelesene, mails sortieren, nach datum sortieren, anhänge anzeigen, mail wiederherstellen, endgültig löschen, als spam markieren, in ordner verschieben, ordner anlegen, gruppenbaum, kundenmails, relevante mails, lesefilter, rechtsklick auf mail, posteingang aktualisieren, warum sehe ich den posteingang nicht]
  en: [inbox, what do I see here, explain this page, what does this folder mean, what does this badge mean, read emails, mail list, spam folder, unread count, sort by date, show attachments, restore email, permanently delete, mark as spam, move to folder, create folder, group tree, client emails, relevant emails, read filter, right click on email, refresh inbox, why can't I see the inbox]
  fr: [boîte de réception, qu'est-ce que je vois ici, explique cette page, que signifie ce dossier, que signifie ce badge, lire les e-mails, liste des e-mails, spam, e-mails non lus, trier par date, pièces jointes, restaurer un e-mail, supprimer définitivement, marquer comme spam, déplacer vers un dossier, créer un dossier, arbre des groupes, e-mails clients, e-mails pertinents, filtre de lecture, actualiser la boîte de réception, pourquoi je ne vois pas la boîte de réception]
  it: [posta in arrivo, cosa vedo qui, spiega questa pagina, cosa significa questa cartella, cosa significa questo badge, leggere le e-mail, elenco e-mail, spam, e-mail non lette, ordinare per data, allegati, ripristinare un'e-mail, eliminare definitivamente, segnare come spam, spostare nella cartella, creare una cartella, albero dei gruppi, e-mail dei clienti, e-mail rilevanti, filtro di lettura, aggiornare la posta in arrivo, perché non vedo la posta in arrivo]
---

# Posteingang — die Seite /workplace/inbox

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

Im **Posteingang** (de: "Posteingang", en: "Inbox", fr: "Boîte de réception", it: "Posta
in arrivo") liest und organisiert der User die E-Mails des verbundenen IMAP-Postfachs in
einem dreispaltigen Mail-Client (Ordner / Mail-Liste / Lesebereich). Erreichbar über das
Briefumschlag-Navi-Icon `open-inbox` (mit rotem Ungelesen-Badge), per Tastenkürzel Alt+9
oder direkt unter `/workplace/inbox`. Icon und Seite erscheinen nur, wenn in den
Einstellungen IMAP-Server, Benutzername und Passwort hinterlegt sind — sonst bleibt das
Icon ausgeblendet und ein direkter Aufruf der Route leitet auf "Kein Zugriff" um. Neue
Mails und Lesestatus-Änderungen treffen live per SignalR ein, ohne Neuladen.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand)

- **Suche**: Das Suchfeld der Kopfleiste wird auf dieser Seite AUSGEBLENDET — für den
  Posteingang gibt es keine Header-Suche (keine SearchStrategy).
- **Gruppen-Auswahl**: Die globale Gruppen-Auswahl wirkt NICHT auf diese Seite (der
  globale Gruppen-Scope gilt nur für Mitarbeiter, Absenzen, Einsatzplan, Dienste und
  Verfügbarkeit). Mails nach Gruppe filtern geht stattdessen über den seiteneigenen
  Abschnitt **Gruppen** in der Ordner-Spalte (siehe unten).

### Aufbau: drei Spalten, Breite per Trennsteg verstellbar

### 1. Spalte: Ordner

- Kopf **Ordner** (de: "Ordner", en: "Folders", fr: "Dossiers", it: "Cartelle") mit einem
  **+**-Button (nur für berechtigte Benutzer sichtbar): öffnet den Eingabedialog
  **Ordnername:** (de: "Ordnername:", en: "Folder name:", fr: "Nom du dossier :", it:
  "Nome cartella:") und legt einen eigenen Ordner an.
- Liste aller Mail-Ordner; Systemordner erscheinen mit lokalisierten Namen:
  **Posteingang** (en: "Inbox", fr: "Boîte de réception", it: "Posta in arrivo"),
  **Spamverdacht** (en: "Spam", fr: "Spam", it: "Spam"), **Papierkorb** (en: "Trash",
  fr: "Corbeille", it: "Cestino"), **Gesendet** (en: "Sent", fr: "Envoyés", it:
  "Inviati") und **Kunden** (en: "Clients", fr: "Clients", it: "Clienti") — in den
  Kunden-Ordner sortiert das System Mails automatisch ein, deren Absender-Adresse zu
  einem erfassten Kunden/Mitarbeiter gehört.
- Badge hinter dem Ordnernamen = Anzahl ungelesener Mails in diesem Ordner (erscheint nur
  bei mindestens einer ungelesenen Mail).
- Eigene (Nicht-System-)Ordner haben ein ✕ zum Löschen (nur für Berechtigte) mit
  Rückfrage **Ordner "{name}" löschen?** (en: "Delete folder \"{name}\"?"). Ohne Ordner
  steht hier **Keine Ordner** (en: "No folders").
- Darunter der Abschnitt **Gruppen** (de: "Gruppen", en: "Groups", fr: "Groupes", it:
  "Gruppi") — erscheint nur, wenn bereits Kunden zugeordnete Mails existieren: ein
  auf-/zuklappbarer Baum (Pfeil-Icons) der Gruppen mit Personen als Unterknoten. Klick
  auf einen Gruppen- oder Personen-Knoten filtert die Mail-Liste auf die Mails dieser
  Gruppe bzw. Person. Das Badge zeigt die Ungelesen-Zahl; bei Knoten mit Unterknoten im
  Format "eigene/gesamt" (Ungelesen-Zahl direkt am Knoten / Summe inkl. aller
  Unterknoten). Personen mit zugeordneten Mails, aber ohne Gruppe, sammelt ein
  Container-Knoten "Unassigned" (derzeit nicht übersetzt).

### 2. Spalte: Mail-Liste

- Kopfzeile: Name des gewählten Ordners, daneben dessen Ungelesen-Zähler und ein
  ↻-Button **Posteingang aktualisieren** (en: "Refresh inbox", fr: "Actualiser la boîte
  de réception", it: "Aggiorna la posta in arrivo") — holt sofort neue Mails vom
  Mailserver ab und lädt Ordner und Liste neu.
- Tabs **Relevant / Sonstiges / Alle** (en: "Relevant"/"Other"/"All", fr:
  "Pertinent"/"Autres"/"Tous", it: "Rilevante"/"Altro"/"Tutti"): **Relevant** blendet
  automatisierte Absender aus (Adressen mit noreply, no-reply, mailer-daemon, postmaster,
  newsletter, notifications, mailings), **Sonstiges** zeigt genau diese, **Alle** zeigt
  alles. Dieser Filter wirkt nur auf die bereits geladenen Mails (clientseitig).
- Dropdown-Lesefilter **Alle / Ungelesen / Gelesen** (en: "All"/"Unread"/"Read", fr:
  "Tous"/"Non lu"/"Lu", it: "Tutti"/"Non letto"/"Letto") plus Sortier-Button **Datum**
  (en: "Date", fr: "Date", it: "Data") mit Pfeil ↓/↑ für ab-/aufsteigend nach
  Empfangsdatum (Standard: neueste zuerst); beide laden die Liste vom Server neu.
- Jede Mail-Zeile zeigt: Ungelesen-Punkt (nur bei ungelesenen Mails, Zeile zusätzlich
  fett), Absender (Name, sonst Adresse), Empfangszeit (Format "TT.MM. HH:mm"), Betreff
  und eine 📎-Büroklammer bei Anhängen. Klick oder Enter öffnet die Mail im Lesebereich.
- Geladen werden bis zu 50 Mails pro Ordner-Ansicht; eine Blätter-Navigation gibt es im
  UI derzeit nicht. Während des Ladens steht **Laden...** (en: "Loading..."), leer zeigt
  die Liste **Keine E-Mails** (en: "No emails", fr: "Aucun e-mail", it: "Nessuna e-mail").
- Rechtsklick auf eine Mail öffnet das Kontextmenü: **Als gelesen markieren** / **Als
  ungelesen markieren** (en: "Mark as read"/"Mark as unread"); **Als Spam markieren**
  (en: "Mark as spam", fr: "Marquer comme spam", it: "Segna come spam") verschiebt in den
  Spamverdacht-Ordner — im Spamverdacht stattdessen **Kein Spam** (en: "Not spam", fr:
  "Pas un spam", it: "Non è spam") zurück in den Posteingang; **In Ordner
  verschieben...** (en: "Move to folder...", fr: "Déplacer vers le dossier...", it:
  "Sposta nella cartella...") mit Untermenü aller Ordner außer dem aktuellen und dem
  Papierkorb; **Löschen** (en: "Delete") verschiebt in den Papierkorb. Im Papierkorb
  bietet das Menü stattdessen **Wiederherstellen** (en: "Restore", fr: "Restaurer", it:
  "Ripristina") und **Endgültig löschen** (en: "Permanently delete", fr: "Supprimer
  définitivement", it: "Elimina definitivamente").

### 3. Spalte: Lesebereich

- Ohne Auswahl steht hier **E-Mail auswählen** (en: "Select an email to view", fr:
  "Sélectionnez un e-mail", it: "Seleziona un'e-mail").
- Kopf mit Betreff und Aktionen: Augen-Icon als Umschalter **Als gelesen markieren / Als
  ungelesen markieren** (en: "Mark read"/"Mark unread"); außerhalb des Papierkorbs ein
  rotes Papierkorb-Icon **Löschen** (en: "Delete"); im Papierkorb stattdessen die Buttons
  **Wiederherstellen** (en: "Restore") und **Endgültig löschen** (en: "Permanently
  delete").
- Meta-Block mit den Zeilen **Von: / An: / Datum: / Ordner:** (en:
  "From:"/"To:"/"Date:"/"Folder:", fr: "De :"/"À :"/"Date :"/"Dossier :", it:
  "Da:"/"A:"/"Data:"/"Cartella:"); das Datum im Format "TT.MM.JJJJ HH:mm:ss".
- Darunter der Mail-Inhalt als bereinigtes HTML (Sanitizing gegen Schadcode); ohne Inhalt
  steht **Kein Inhalt** (en: "No content", fr: "Aucun contenu", it: "Nessun contenuto").
- Das Öffnen einer ungelesenen Mail markiert sie automatisch als gelesen; alle
  Ungelesen-Badges (Ordner, Gruppen, Navi-Icon) aktualisieren sich mit.

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **IMAP-Spiegelung**: Jede Aktion auf dieser Seite wird auf dem Mailserver
  nachgezogen — Lesestatus setzt das Gelesen-Flag auf dem Server, Verschieben/Spam/
  Löschen verschieben die Mail auch im IMAP-Postfach, **Endgültig löschen** entfernt sie
  endgültig vom Server. **Wiederherstellen** legt die Mail zurück in den Posteingang
  (nicht zwingend in den ursprünglichen Ordner).
- **Mail-Abruf (Polling)**: Ein Hintergrunddienst des Backends holt neue Mails in festen
  Abständen vom IMAP-Server (Standard alle 300 Sekunden; einstellbar über
  **Abfrageintervall (Sekunden)** (de: "Abfrageintervall (Sekunden)", en: "Poll Interval
  (Seconds)", fr: "Intervalle d'interrogation (secondes)", it: "Intervallo di polling
  (secondi)") in der Einstellungs-Karte **Eingehender Mailserver (IMAP)** (en: "Incoming
  Mail Server (IMAP)", fr: "Serveur de messagerie entrant (IMAP)", it: "Server di posta
  in arrivo (IMAP)")). Der ↻-Button holt sofort ab. Jede neue Mail im Posteingang
  durchläuft automatisch zwei Schritte: 1. **Spam-Klassifikation** (Regeln + optional
  LLM-Bewertung) — als Spam erkannte Mails wandern in den Spamverdacht, auch auf dem
  Server; 2. **Kunden-Zuordnung** — ist die Absender-Adresse einem erfassten
  Kunden/Mitarbeiter zugeordnet, landet die Mail im Ordner **Kunden** und speist den
  Gruppen-Baum.
- **Live-Updates (SignalR)**: Neue Mails aktualisieren Ordnerliste und Badges sofort;
  Lesestatus-Änderungen erscheinen unmittelbar in Liste und Lesebereich — auch über
  mehrere offene Browser/Tabs hinweg.
- **Ungelesen-Zähler**: Das rote Badge am Navi-Icon `open-inbox` zeigt die
  Gesamtzahl ungelesener Mails; die Ordner-Badges zählen pro Ordner, die
  Gruppen-Badges pro Knoten (bei Knoten mit Unterknoten "eigene/gesamt").
- **Sichtbarkeit & Berechtigungen (falls sichtbar)**: Seite und Navi-Icon existieren nur
  bei konfiguriertem IMAP-Postfach (Server, Benutzername, Passwort in den
  Einstellungen) — ohne Konfiguration verschwindet das Icon aus der Navigation und ein
  direkter Routen-Aufruf landet auf "Kein Zugriff". Ordner anlegen (+) und Ordner löschen
  (✕) sind zusätzlich nur für berechtigte Benutzer sichtbar; Lesen, Verschieben und
  Löschen von Mails sind nicht gesondert berechtigungsgesteuert.
- **Gruppen-Scope-Isolation**: Die globale Gruppen-Auswahl der Kopfleiste hat keinerlei
  Wirkung auf den Posteingang (und umgekehrt) — der Gruppen-Baum dieser Seite ist ein
  eigener, lokaler Filter.
- **Periodenabschluss & Szenarien**: keine Wechselwirkung — E-Mails unterliegen weder
  dem Periodenabschluss noch der Szenario-Isolation des Planungs-Assistenten.

### Typische Aufgaben

- Neue Mails durchsehen → Ordner Posteingang, Tab Relevant; per Chat: `list_emails`
  (Übersicht) und `read_email` (einzelne Mail lesen).
- Ordnerstruktur und Ungelesen-Stände abfragen → Spalte Ordner; per Chat:
  `list_email_folders`.
- Unerwünschte Mails wegräumen → Rechtsklick "Als Spam markieren" bzw. "Löschen";
  Fehlgriffe im Papierkorb über "Wiederherstellen" zurückholen.
- Mails einer Gruppe oder einer Person gebündelt sehen → Gruppen-Baum in der
  Ordner-Spalte anklicken (NICHT die globale Gruppen-Auswahl — die wirkt hier nicht).
- Spam-Filter justieren (Schwellwerte, LLM-Filter) → per Chat
  `update_spam_filter_settings`; die Spam-Regeln-Karte **Spam-Regeln** (en: "Spam
  Rules", fr: "Règles anti-spam", it: "Regole antispam") liegt auf der
  Einstellungs-Seite.
- Zur Seite springen → Skill `navigate_to` mit Ziel "inbox".

### Verwandte Seiten

- **Einstellungen** (`/workplace/settings`, Bereich Kommunikation & Berichte): dort
  werden das IMAP-Konto (Karte "Eingehender Mailserver (IMAP)" inkl. Abfrageintervall
  und Verbindungstest), SMTP und die Spam-Regeln konfiguriert — ohne IMAP-Konto bleibt
  der Posteingang unsichtbar.
- `explain_email_setup` — Schritt-für-Schritt-Einrichtung von SMTP/IMAP (Provider,
  Ports, SSL/TLS).
- `explain_page_settings_overview` — Überblick über die Einstellungs-Seite.

### Trigger-Phrasen

- "Was sehe ich im Posteingang?"
- "Warum sehe ich das Mail-Icon nicht?" (IMAP nicht konfiguriert)
- "Was bedeutet das Badge am Ordner?"
- "Wie markiere ich eine Mail als Spam?"
- "How do I restore an email from the trash?"
