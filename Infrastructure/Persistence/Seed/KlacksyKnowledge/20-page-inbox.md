---
name: explain_page_inbox
description: |
  Explains the Inbox page (Posteingang) at /workplace/inbox: the three-column mail layout
  with the Folders column (system folders Inbox/Spam/Trash/Sent/Clients plus user folders
  and the Groups tree for filtering mails by group or person), the email list with the
  Relevant/Other/All tabs, read filter, date sort and right-click context menu (mark
  read/unread, mark as spam, move to folder, delete/restore), and the reading pane with
  From/To/Date/Folder metadata and actions. Use this when the user asks what they see on
  the Inbox page, what the columns/folders/tabs/badges mean, or how to work with it.
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
  - ungelesen
  - unread
  - papierkorb
synonyms:
  de: [posteingang, was sehe ich hier, erkläre diese seite, was bedeutet dieser ordner, was bedeutet dieses badge, e-mails lesen, mail-liste, spamverdacht]
  en: [inbox, what do I see here, explain this page, what does this folder mean, what does this badge mean, read emails, mail list, spam folder]
  fr: [boîte de réception, qu'est-ce que je vois ici, explique cette page, que signifie ce dossier, lire les e-mails, liste des e-mails, spam]
  it: [posta in arrivo, cosa vedo qui, spiega questa pagina, cosa significa questa cartella, leggere le e-mail, elenco e-mail, spam]
---

# Posteingang — die Seite /workplace/inbox

## Zweck (1 Satz)

Im **Posteingang** (de: "Posteingang", en: "Inbox", fr: "Boîte de réception", it: "Posta in arrivo") liest und organisiert der User die eingehenden E-Mails des verbundenen IMAP-Postfachs — erreichbar über das Briefumschlag-Navi-Icon `open-inbox` (mit rotem Ungelesen-Badge), per Tastenkürzel Alt+9 oder direkt unter `/workplace/inbox`. Das Icon und die Seite erscheinen nur, wenn in den Einstellungen IMAP-Server, Benutzername und Passwort hinterlegt sind (sonst leitet die Seite auf "Kein Zugriff" um). Neue Mails und Lesestatus-Änderungen treffen live per SignalR ein, ohne Neuladen.

## Aufbau: drei Spalten (Breite per Trennsteg verstellbar)

### 1. Spalte **Ordner** (de: "Ordner", en: "Folders", fr: "Dossiers", it: "Cartelle")

- Liste aller Mail-Ordner; Systemordner erscheinen mit lokalisierten Namen: **Posteingang** (en: "Inbox", fr: "Boîte de réception", it: "Posta in arrivo"), **Spamverdacht** (en: "Spam", fr: "Spam", it: "Spam"), **Papierkorb** (en: "Trash", fr: "Corbeille", it: "Cestino"), **Gesendet** (en: "Sent", fr: "Envoyés", it: "Inviati") und **Kunden** (en: "Clients", fr: "Clients", it: "Clienti") für automatisch Kunden zugeordnete Mails.
- Badge hinter dem Ordnernamen = Anzahl ungelesener Mails in diesem Ordner.
- "+"-Button im Kopf (nur für Berechtigte): legt über den Eingabedialog **Ordnername:** (en: "Folder name:") einen eigenen Ordner an; eigene (Nicht-System-)Ordner haben ein ✕ zum Löschen mit Rückfrage **Ordner "{name}" löschen?** (en: "Delete folder \"{name}\"?").
- Darunter der Abschnitt **Gruppen** (de: "Gruppen", en: "Groups", fr: "Groupes", it: "Gruppi"): ein auf-/zuklappbarer Baum der Gruppen mit Personen als Unterknoten. Klick auf einen Gruppen- oder Personen-Knoten filtert die Mail-Liste auf E-Mails dieser Gruppe bzw. Person. Das Badge zeigt die Ungelesen-Zahl; bei Knoten mit Kindern im Format "eigene/gesamt" (z. B. "2/7" = 2 direkt am Knoten, 7 inkl. aller Unterknoten).

### 2. Spalte: Mail-Liste

- Kopfzeile: Name des gewählten Ordners, daneben dessen Ungelesen-Zähler und ein ↻-Button **Posteingang aktualisieren** (en: "Refresh inbox", fr: "Actualiser la boîte de réception", it: "Aggiorna la posta in arrivo") — holt sofort neue Mails vom Mailserver ab und lädt die Liste neu.
- Tabs **Relevant / Sonstiges / Alle** (en: "Relevant"/"Other"/"All", fr: "Pertinent"/"Autres"/"Tous", it: "Rilevante"/"Altro"/"Tutti"): **Relevant** blendet automatisierte Absender aus (Adressen mit noreply, no-reply, mailer-daemon, postmaster, newsletter, notifications, mailings), **Sonstiges** zeigt genau diese, **Alle** zeigt alles.
- Dropdown-Lesefilter **Alle / Ungelesen / Gelesen** (en: "All"/"Unread"/"Read") plus Sortier-Button **Datum** (en: "Date") mit Pfeil ↓/↑ für ab-/aufsteigend nach Empfangsdatum.
- Jede Mail-Zeile zeigt: Ungelesen-Punkt (nur bei ungelesenen Mails, Zeile zusätzlich fett), Absender (Name, sonst Adresse), Empfangszeit (Format "TT.MM. HH:mm"), Betreff und eine 📎-Büroklammer bei Anhängen. Geladen werden 50 Mails pro Seite; leer zeigt die Liste **Keine E-Mails** (en: "No emails").
- Rechtsklick auf eine Mail öffnet das Kontextmenü: **Als gelesen markieren** / **Als ungelesen markieren** (en: "Mark as read"/"Mark as unread"); **Als Spam markieren** (en: "Mark as spam") verschiebt in den Spamverdacht-Ordner — dort stattdessen **Kein Spam** (en: "Not spam") zurück in den Posteingang; **In Ordner verschieben...** (en: "Move to folder...") mit Untermenü aller Ordner außer Papierkorb; **Löschen** (en: "Delete") verschiebt in den Papierkorb. Im Papierkorb bietet das Menü **Wiederherstellen** (en: "Restore") und **Endgültig löschen** (en: "Permanently delete").

### 3. Spalte: Lesebereich

- Ohne Auswahl steht hier **E-Mail auswählen** (en: "Select an email to view", fr: "Sélectionnez un e-mail", it: "Seleziona un'e-mail").
- Kopf mit Betreff und Aktionen: Augen-Icon als Umschalter **Als gelesen markieren / Als ungelesen markieren** (en: "Mark read"/"Mark unread"); außerhalb des Papierkorbs ein rotes Papierkorb-Icon **Löschen** (en: "Delete"); im Papierkorb stattdessen die Buttons **Wiederherstellen** (en: "Restore") und **Endgültig löschen** (en: "Permanently delete").
- Meta-Block mit den Zeilen **Von: / An: / Datum: / Ordner:** (en: "From:"/"To:"/"Date:"/"Folder:", fr: "De :"/"À :"/"Date :"/"Dossier :", it: "Da:"/"A:"/"Data:"/"Cartella:"); das Datum im Format "TT.MM.JJJJ HH:mm:ss".
- Darunter der Mail-Inhalt als bereinigtes HTML; ohne Inhalt **Kein Inhalt** (en: "No content").
- Das Öffnen einer ungelesenen Mail markiert sie automatisch als gelesen; alle Ungelesen-Badges (Ordner, Gruppen, Navi-Icon) aktualisieren sich mit.

## Typische Aufgaben auf dieser Seite

- Neue Mails durchsehen → Ordner Posteingang, Tab Relevant; per Chat: `list_emails` (Übersicht) und `read_email` (einzelne Mail lesen).
- Ordnerstruktur und Ungelesen-Stände abfragen → Spalte Ordner; per Chat: `list_email_folders`.
- Unerwünschte Mails wegräumen → Rechtsklick "Als Spam markieren" bzw. "Löschen"; Fehlgriffe im Papierkorb wiederherstellen.
- Mails einer Gruppe oder eines Kunden gebündelt sehen → Gruppen-Baum in der Ordner-Spalte anklicken.
- Spam-Filter justieren (Schwellwerte, LLM-Filter) → per Chat `get_spam_filter_settings` / `update_spam_filter_settings`; die Spam-Regeln-Karte liegt auf der Einstellungs-Seite.
- Zur Seite springen → Skill `navigate_to` mit Ziel inbox.

## Verwandte Seiten und Happen

- **Einstellungen** (`/workplace/settings`): dort werden IMAP/SMTP-Konto und die Spam-Regeln konfiguriert — ohne IMAP-Konto bleibt der Posteingang unsichtbar.
- `explain_email_setup` — Schritt-für-Schritt-Einrichtung von SMTP/IMAP (Provider, Ports, SSL/TLS).
