---
name: explain_page_settings_overview
description: |
  Explains the Klacks settings page (/workplace/settings) — the admin-only configuration hub
  reached via the gear icon (open-settings, Alt+8) in the main navigation. Gives an orientation
  overview of its twelve collapsible sections: General (app name, owner address, data
  retention/GDPR), Users & Access (user administration, group visibility per user, identity
  providers), Organization (branches, contracts, states, countries, qualifications),
  Work & Scheduling (work settings, scheduling defaults, scheduling rules, Holistic Harmonizer),
  Absence & Calendar (absence types, absence details, holiday rules, calendar selection),
  Communication & Reports (SMTP email, IMAP, spam rules, report templates and defaults),
  Appearance & Automation (grid colors, macros, floor plan), AI / LLM (models, providers,
  sync log, model check), Klacksy (speech, personality, skill proposals, autonomy),
  External Services (OpenRoute, DeepL, messaging), Plugins (language packs, feature plugins)
  and System (software updates). Also explains that Klacksy's guided first-run setup tour walks
  through the most important cards. Use this when the user asks what they see on the settings
  page, what the sections/cards mean, or how to work with it.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - settings
  - einstellungen
  - konfiguration
  - configuration
  - settings page
  - einstellungsseite
  - admin settings
  - setup
  - sektionen
  - sections
synonyms:
  de: [einstellungen, einstellungsseite, konfiguration, was sehe ich hier, erkläre diese seite, was bedeutet diese karte, welche einstellungen gibt es, wo finde ich die einstellung]
  en: [settings, settings page, configuration, what do i see here, explain this page, what does this card mean, where do i find a setting]
  fr: [paramètres, page des paramètres, configuration, que vois-je ici, explique cette page, que signifie cette carte, où trouver un paramètre]
  it: [impostazioni, pagina delle impostazioni, configurazione, cosa vedo qui, spiega questa pagina, cosa significa questa scheda, dove trovo un'impostazione]
---

# Einstellungen — die Konfigurations-Zentrale von Klacks

## Zweck + Weg dorthin (1 Satz)

Die Seite **Einstellungen** (de: "Einstellungen", en: "Settings", fr: "Paramètres",
it: "Impostazioni") unter `/workplace/settings` bündelt die gesamte systemweite
Konfiguration von Klacks — erreichbar über das Zahnrad-Icon rechts oben in der
Hauptnavigation (`open-settings`, Tastenkürzel Alt+8). Die Seite ist **nur für
Administratoren** sichtbar (Route mit AdminGuard geschützt, das Icon erscheint nur
für Admins).

## Aufbau

Oben der Seitentitel mit zwei Knöpfen zum Auf- und Zuklappen aller Abschnitte.
Darunter zwölf **Sektionen** (Kartengruppen), jede einzeln per Klick auf die
Abschnitts-Zeile auf-/zuklappbar. Dies ist nur die Orientierung — jede Karte hat
eigene Felder und Dialoge; bei Fragen zu einer einzelnen Karte einfach nachfragen.

1. **Allgemein** (de: "Allgemein", en: "General", fr: "Général", it: "Generale"):
   Grunddaten der Installation — App-Name und Allgemeines, die Firmen-/Sekretariats-Adresse
   ("Adresse Sekretariat") sowie Datenschutz/Aufbewahrungsfristen ("Datenschutz (DSGVO)").
2. **Benutzer & Zugriff** (de: "Benutzer & Zugriff", en: "Users & Access",
   fr: "Utilisateurs & Accès", it: "Utenti & Accesso"): Login-Konten in der
   Benutzerverwaltung, die Sichtbarkeit von Gruppen pro Benutzer (Group Scope) und
   externe Identity Provider für die Anmeldung.
3. **Organisation** (de: "Organisation", en: "Organization", fr: "Organisation",
   it: "Organizzazione"): Stammdaten des Betriebs — Filialen, Verträge (Vertragsvorlagen),
   Staaten/Kantone, Länder und Qualifikationen der Mitarbeiter.
4. **Arbeitszeit & Planung** (de: "Arbeitszeit & Planung", en: "Work & Scheduling",
   fr: "Travail & Planification", it: "Lavoro & Pianificazione"): Anstellungs- und
   Lohnparameter (Ferientage, Probezeit, Kündigungsfrist, Zahlungsintervall),
   Planungs-Standardwerte, Planungsregeln (Arbeitszeit-Limits, Zuschläge) und die
   Wizard-Einstellungen des "Holistic Harmonizer".
5. **Abwesenheit & Kalender** (de: "Abwesenheit & Kalender", en: "Absence & Calendar",
   fr: "Absences & Calendrier", it: "Assenze & Calendario"): Absenztypen,
   Abwesenheitsdetails, Feiertags-/Kalenderregeln und die Kalenderauswahl
   (welche Feiertagskalender gelten).
6. **Kommunikation & Berichte** (de: "Kommunikation & Berichte",
   en: "Communication & Reports", fr: "Communication & Rapports",
   it: "Comunicazione & Report"): ausgehende E-Mail (SMTP), eingehender Mailserver
   (IMAP), Spam-Regeln sowie Report-Vorlagen und Report-Standardwerte.
7. **Darstellung & Automatisierung** (de: "Darstellung & Automatisierung",
   en: "Appearance & Automation", fr: "Affichage & Automatisation",
   it: "Aspetto & Automazione"): Tabellenfarben des Plans, Berechnungs-Macros
   und der Grundriss (Floor Plan).
8. **KI / LLM** (de: "KI / LLM", en: "AI / LLM", fr: "IA / LLM", it: "IA / LLM"):
   LLM-Modelle, LLM-Anbieter (API-Keys), das Sync-Protokoll und der Check
   "Beste Klacksy-Modelle".
9. **Klacksy** (in allen Sprachen "Klacksy"): Sprach-Einstellungen (Stimme/Diktat),
   Persönlichkeit, Skill-Vorschläge und die Autonomie-Stufe des Assistenten.
10. **Externe Dienste** (de: "Externe Dienste", en: "External Services",
    fr: "Services externes", it: "Servizi esterni"): OpenRoute (Routing/Geodaten) und
    DeepL (Übersetzung); ist das Messaging-Plugin aktiviert, zusätzlich
    Messaging-Provider und eigene Messenger-Konten.
11. **Plugins** (in allen Sprachen "Plugins"): Sprachpakete installieren/entfernen
    und Feature-Plugins ein-/ausschalten.
12. **System** (de: "System", en: "System", fr: "Système", it: "Sistema"):
    Software-Updates der Installation.

## Geführte Einrichtung durch Klacksy

Beim ersten Start bietet Klacksy eine **geführte Einrichtungs-Tour** an, die durch die
wichtigsten Karten führt: App-Titel und Firmenadresse fragt Klacksy direkt im Chat ab
und trägt sie ein; Kalenderauswahl, Benutzerverwaltung, Gruppen-Sichtbarkeit, Identity
Provider, Planung, Absenzen, Feiertage, E-Mail, LLM/Klacksy und Plugins werden gezeigt
bzw. erklärt. Dabei pulsiert jeweils das passende Navigations-Icon, damit der Weg zur
Seite gelernt wird.

## Typische Aufgaben + passende Klacksy-Skills

- App-Name oder Firmenadresse ändern → `get_general_settings`, `update_general_settings`,
  `get_owner_address`, `update_owner_address`
- Benutzer anlegen und Rechte/Sichtbarkeit setzen → `create_user`, `list_system_users`,
  `assign_user_permissions`, `set_user_group_scope`
- E-Mail einrichten und testen → `update_email_settings`, `update_imap_settings`,
  `test_smtp_connection`, `test_imap_connection`
- Planungsregeln und Standardwerte pflegen → `list_scheduling_rules`,
  `create_scheduling_rule`, `get_scheduling_defaults`, `update_scheduling_defaults`,
  `update_work_settings`
- LLM-Anbieter und -Modelle verwalten → `list_llm_providers`, `create_llm_provider`,
  `list_llm_models`, `create_llm_model`
- Zur Seite springen oder ein Element finden → `navigate_to`, `get_page_controls`

## Verwandte Seiten

- Profil (`/workplace/profile`) — persönliche Einstellungen des angemeldeten Benutzers,
  im Gegensatz zur systemweiten Konfiguration hier.
- Gruppen (`/workplace/group`) — die Gruppen selbst werden dort gepflegt; hier wird nur
  ihre Sichtbarkeit pro Benutzer gesteuert.
- Schichtplan (`/workplace/schedule`) — nutzt die hier definierten Planungsregeln,
  Macros, Tabellenfarben und Kalender.
