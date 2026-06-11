---
name: explain_page_settings_overview
description: |
  Explains the Klacks settings page (/workplace/settings) — the admin-only configuration hub
  reached via the gear icon (open-settings, Alt+8) in the main navigation; route and icon are
  only available with admin permission. Gives an orientation overview of its twelve collapsible
  sections: General (app name, owner address, data retention/GDPR), Users & Access (user
  administration, group visibility per user, identity providers), Organization (branches,
  contracts, states, countries, qualifications), Work & Scheduling (work settings, scheduling
  defaults, scheduling rules, Holistic Harmonizer), Absence & Calendar (absence types, absence
  details, holiday rules, calendar selection), Communication & Reports (SMTP email, IMAP, spam
  rules, report templates and defaults), Appearance & Automation (grid colors, calculation
  macros, floor plan), AI / LLM (models, providers, sync log, model check), Klacksy (speech,
  personality, skill proposals, autonomy), External Services (OpenRoute, DeepL, messaging when
  the plugin is enabled), Plugins (language packs, feature plugins) and System (software
  updates). Also covers Klacksy's guided first-run setup tour and how the settings feed other
  pages (absence types → absence calendar, contracts and scheduling rules → resource monitor
  and planning, LLM providers → Klacksy, macros → booked services, email/IMAP → inbox).
  Use this when the user asks what they see on the settings page, what the sections/cards
  mean, or how to work with it. Supports a level parameter: short (purpose only), elements
  (every section and card explained), effects (how the settings affect the other pages).
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
  - settings overview
  - einstellungen übersicht
  - harmonizer
  - holistic harmonizer
  - harmonizer einstellungen
  - planungsregeln
  - makro einstellungen
  - macro settings
  - openroute
  - deepl
  - zahnrad
  - gear icon
synonyms:
  de: [einstellungen, einstellungsseite, konfiguration, was sehe ich hier, erkläre diese seite, was bedeutet diese karte, welche einstellungen gibt es, wo finde ich die einstellung, harmonizer einstellungen, planungsregeln einstellen, automatisierungs-makros, wo stelle ich den llm-anbieter ein, e-mail einrichten, zahnrad-symbol]
  en: [settings, settings page, configuration, what do i see here, explain this page, what does this card mean, where do i find a setting, holistic harmonizer settings, scheduling rule configuration, macro automation, where do i set the llm provider, set up email, gear icon]
  fr: [paramètres, page des paramètres, configuration, que vois-je ici, explique cette page, que signifie cette carte, où trouver un paramètre, harmoniseur holistique, paramètres de règles de planification, macros d'automatisation, où configurer le fournisseur llm, configurer l'e-mail, icône d'engrenage]
  it: [impostazioni, pagina delle impostazioni, configurazione, cosa vedo qui, spiega questa pagina, cosa significa questa scheda, dove trovo un'impostazione, armonizzatore olistico, impostazioni delle regole di pianificazione, automazione macro, dove imposto il provider llm, configurare l'e-mail, icona dell'ingranaggio]
---

# Einstellungen — die Seite /workplace/settings

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

Die Seite **Einstellungen** (de: "Einstellungen", en: "Settings", fr: "Paramètres",
it: "Impostazioni") unter `/workplace/settings` bündelt die gesamte systemweite
Konfiguration von Klacks in zwölf auf- und zuklappbaren Sektionen — von Grunddaten der
Installation über Planungsregeln und Absenztypen bis zu LLM-Anbietern, Klacksy und
Software-Updates. Erreichbar — falls Berechtigung — über das Zahnrad-Icon
`open-settings` rechts oben in der Hauptnavigation (Alt+8); die Seite ist **nur für
Administratoren** zugänglich (Route mit AdminGuard geschützt, das Zahnrad-Icon
erscheint nur für Admins). Dies ist die Orientierungs-Übersicht — jede Karte hat
eigene Felder und Dialoge; bei Fragen zu einer einzelnen Karte einfach nachfragen.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand)

- **Suche**: Auf dieser Seite ist das Suchfeld der Kopfleiste **ausgeblendet** — die
  Einstellungsseite hat keine Such-Strategie (die Suche gilt nur für Listen-Seiten wie
  Mitarbeiter, Absenzen, Schichtplan, Schichten, Gruppen und Verfügbarkeit).
- **Gruppen-Auswahl**: Der globale Gruppen-Scope hat auf dieser Seite **keine Wirkung** —
  Einstellungen sind systemweite Stammdaten und nicht gruppen-gefiltert (der Gruppen-Scope
  wirkt nur auf Mitarbeiter, Absenzen, Schichtplan, Schichten und Verfügbarkeit). Nicht zu
  verwechseln mit der Karte **Gruppen-Sichtbarkeit** (`group-scope`) weiter unten, die
  festlegt, welche Gruppen ein Benutzer überhaupt sehen darf.
- Auch die globale Speicherleiste (Savebar) ist hier ausgeblendet — jede Karte speichert
  ihre Änderungen selbst.

### Kopfzeile der Seite

Oben der Seitentitel **Einstellungen** (de: "Einstellungen", en: "Settings",
fr: "Paramètres", it: "Impostazioni") mit zwei Icon-Knöpfen rechts: **alle Sektionen
aufklappen** und **alle Sektionen zuklappen**.

### Die zwölf Sektionen

Jede Sektion ist eine Kartengruppe mit eigener Abschnitts-Zeile; Klick auf die Zeile
(oder Enter, die Zeilen sind per Tastatur erreichbar) klappt sie auf oder zu, ein
Pfeil-Icon zeigt den Zustand. Die Karten tragen Anker-IDs (unten in Backticks), über
die Klacksy mit `get_page_controls` und in der geführten Tour direkt zu einer Karte
scrollen kann.

1. **Allgemein** (de: "Allgemein", en: "General", fr: "Général", it: "Generale"):
   `settings-general` (App-Name und Allgemeines), `owner-address` (Firmen-/
   Sekretariats-Adresse), `data-retention` (Datenschutz/DSGVO, Aufbewahrungsfristen).
2. **Benutzer & Zugriff** (de: "Benutzer & Zugriff", en: "Users & Access",
   fr: "Utilisateurs & Accès", it: "Utenti & Accesso"): `user-management`
   (Login-Konten und Rechte), `group-scope` (Sichtbarkeit von Gruppen pro Benutzer),
   `identity-providers` (externe Identity Provider für die Anmeldung).
3. **Organisation** (de: "Organisation", en: "Organization", fr: "Organisation",
   it: "Organizzazione"): `branches` (Filialen), `contracts` (Vertragsvorlagen),
   `states` (Staaten/Kantone), `countries` (Länder), `qualifications`
   (Qualifikationen der Mitarbeitenden).
4. **Arbeitszeit & Planung** (de: "Arbeitszeit & Planung", en: "Work & Scheduling",
   fr: "Travail & Planification", it: "Lavoro & Pianificazione"): `work-setting`
   (Anstellungs- und Lohnparameter wie Ferientage, Probezeit, Kündigungsfrist,
   Zahlungsintervall), `scheduling-defaults` (Planungs-Standardwerte, z. B.
   Standard-Arbeitstage), `scheduling-rules` (Planungsregeln: Arbeitszeit-Limits,
   Zuschläge), `wizard` (Einstellungen des **Holistic Harmonizer**, des
   Schichtplan-Wizards — Label in allen Sprachen "Holistic Harmonizer").
5. **Abwesenheit & Kalender** (de: "Abwesenheit & Kalender", en: "Absence & Calendar",
   fr: "Absences & Calendrier", it: "Assenze & Calendario"): `absence-types`
   (Absenztypen: Name, Kürzel, Farbe, Standardwerte), `absence-detail`
   (Abwesenheitsdetails), `calendar-rules` (Feiertags-/Kalenderregeln),
   `calendar-selection` (Kalenderauswahl — welche Feiertagskalender gelten).
6. **Kommunikation & Berichte** (de: "Kommunikation & Berichte",
   en: "Communication & Reports", fr: "Communication & Rapports",
   it: "Comunicazione & Report"): `email-config` (ausgehende E-Mail/SMTP),
   `imap-setting` (eingehender Mailserver/IMAP), `spam-rules` (Spam-Regeln),
   `reports` (Report-Vorlagen), `report-defaults` (Report-Standardwerte).
7. **Darstellung & Automatisierung** (de: "Darstellung & Automatisierung",
   en: "Appearance & Automation", fr: "Affichage & Automatisation",
   it: "Aspetto & Automazione"): `grid-color` (Tabellenfarben des Plans),
   `macros` (Berechnungs-Makros), `floor-plan-settings` (Grundriss/Floor Plan).
8. **KI / LLM** (de: "KI / LLM", en: "AI / LLM", fr: "IA / LLM", it: "IA / LLM"):
   `llm-models` (LLM-Modelle), `llm-provider` (LLM-Anbieter mit API-Keys),
   `llm-sync-log` (Sync-Protokoll), `klacksy-model-check` (Check "Beste
   Klacksy-Modelle").
9. **Klacksy** (in allen Sprachen "Klacksy"): `assistant-speech` (Stimme/Diktat),
   `assistant-personality` (Persönlichkeit), `assistant-skill-proposals`
   (Skill-Vorschläge), `klacksy-autonomy` (Autonomie-Stufe des Assistenten).
10. **Externe Dienste** (de: "Externe Dienste", en: "External Services",
    fr: "Services externes", it: "Servizi esterni"): `openroute` (Routing/Geodaten),
    `deepl` (Übersetzung). Ist das Messaging-Feature-Plugin aktiviert, erscheinen
    zusätzlich `messaging-providers` (Messaging-Anbieter) und `owner-messengers`
    (eigene Messenger-Konten) — sonst fehlen diese beiden Karten.
11. **Plugins** (in allen Sprachen "Plugins"): `language-plugins` (Sprachpakete
    installieren/entfernen), `feature-plugins` (Feature-Plugins ein-/ausschalten).
12. **System** (de: "System", en: "System", fr: "Système", it: "Sistema"):
    `updates` (Software-Updates der Installation).

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Berechtigung**: Die ganze Seite ist Admin-only (AdminGuard auf der Route); wer das
  Zahnrad-Icon nicht sieht, hat keine Admin-Berechtigung. Einstellungen sind globale
  Stammdaten — der Gruppen-Scope und die Szenario-Isolation (AnalyseToken) der
  Planungsseiten spielen hier keine Rolle.
- **Absenztypen → Absenzen-Kalender** (`/workplace/absence`): Name, Kürzel, Farbe und
  Standardwerte der Absenztypen bestimmen die Chips der Legende, die Balkenfarben und
  neue Einträge im Kalender — Änderungen wirken sofort. Kalender-Einträge selbst sind
  vorgeplante Platzhalter (Erinnerungsstützen); verbindlich wird eine Absenz erst als
  gebuchter Absenz-Dienst in der Einsatzplanung.
- **Verträge, Planungsregeln, Planungs-Standardwerte → Ressourcen-Monitor + Planung**:
  Die Wunsch-/Max-Linien des Ressourcen-Monitors auf dem Dashboard werden pro
  Mitarbeiter aus den aktiven Verträgen (Arbeitstage-Muster, Limits der verknüpften
  Planungsregel) berechnet; ohne aktiven Vertrag gelten die Planungs-Standardwerte und
  Limits dieser Seite als Fallback. Der Dienste-Balken zählt geplante Dienste
  (Container = 1; sporadische Dienste, Zeitraum-Schichten, versiegelte Aufträge und
  Szenario-Einträge zählen nicht); der Absenzen-Balken umfasst vorgeplante Platzhalter
  und gebuchte Absenz-Dienste. Planungsregeln und Harmonizer-Einstellungen steuern
  zudem die Vorschläge des Schichtplan-Wizards.
- **LLM-Anbieter und -Modelle → Klacksy**: Die hier hinterlegten API-Keys und das
  gewählte Standard-Modell bestimmen, welches Sprachmodell im Klacksy-Chat antwortet;
  das Sync-Protokoll und der Modell-Check helfen bei der Auswahl. Die Klacksy-Sektion
  steuert Stimme, Persönlichkeit und die Autonomie-Stufe des Assistenten.
- **Makros → Einsatzplanung**: Berechnungs-Makros (z. B. Zuschläge) wertet das Backend
  beim Buchen und Ändern von Diensten und Absenz-Diensten aus; Planungsregel-Makros
  fließen in die Regelprüfung des Plans ein.
- **E-Mail/IMAP/Spam → Posteingang** (`/workplace/inbox`): SMTP konfiguriert den
  Mailversand, IMAP den Abruf des Posteingangs, die Spam-Regeln dessen Filterung.
- **Tabellenfarben** (`grid-color`) wirken auf den Schichtplan und die
  Dashboard-Diagramme; **Kalenderregeln/Kalenderauswahl** bestimmen, welche Feiertage
  in Absenzen-Kalender, Schichtplan und Ressourcen-Monitor markiert sind.
- **Feature-Plugins** schalten ganze Funktionsbereiche frei — z. B. blendet das
  Messaging-Plugin die Messaging-Karten in "Externe Dienste" und die Messaging-Seite
  ein; **Sprachpakete** ergänzen weitere UI-Sprachen.
- **Geführte Einrichtung durch Klacksy**: Beim ersten Start bietet Klacksy eine
  geführte Einrichtungs-Tour an (Skill `start_guided_tour`), die durch die wichtigsten
  Karten führt: App-Titel und Firmenadresse fragt Klacksy direkt im Chat ab und trägt
  sie ein; Kalenderauswahl, Benutzerverwaltung, Gruppen-Sichtbarkeit, Identity
  Provider, Planung, Mitarbeiter, Schichten, Verfügbarkeit, Absenzen, Feiertage,
  Periodenabschluss, E-Mail, LLM/Klacksy und Plugins werden gezeigt bzw. erklärt.
  Dabei pulsiert jeweils das passende Navigations-Icon, damit der Weg zur Seite
  gelernt wird.

### Typische Aufgaben

- App-Name oder Firmenadresse ändern — Skills `get_general_settings`,
  `update_general_settings`, `get_owner_address`, `update_owner_address`
- Benutzer anlegen und Rechte/Sichtbarkeit setzen — Skills `create_user`,
  `list_system_users`, `assign_user_permissions`, `set_user_group_scope`
- Identity Provider verwalten — Skills `list_identity_providers`,
  `create_identity_provider`, `update_identity_provider`
- E-Mail einrichten und testen — Skills `update_email_settings`,
  `update_imap_settings`, `test_smtp_connection`, `test_imap_connection`; Spam-Filter
  — `get_spam_filter_settings`, `update_spam_filter_settings`
- Planungsregeln und Standardwerte pflegen — Skills `list_scheduling_rules`,
  `create_scheduling_rule`, `update_scheduling_rule`, `get_scheduling_defaults`,
  `update_scheduling_defaults`, `update_work_settings`
- Verträge und Qualifikationen pflegen — Skills `list_contracts`, `create_contract`,
  `update_contract`, `list_qualifications`, `create_qualification`
- Absenztypen ansehen/anlegen/ändern — Skills `list_absence_types`, `create_absence`,
  `update_absence`
- Feiertage und Kalender — Skills `import_calendar_rules`, `validate_calendar_rule`,
  `list_holidays_for_period`
- Makros verwalten — Skills `list_macros`, `create_macro`, `update_macro`,
  `explain_macro_editor`
- LLM-Anbieter und -Modelle verwalten — Skills `list_llm_providers`,
  `create_llm_provider`, `list_llm_models`, `create_llm_model`
- Klacksy konfigurieren — Skills `get_speech_settings`, `get_autonomy_level`,
  `set_autonomy_level`
- DeepL-Übersetzung einrichten — Skills `get_deepl_settings`, `update_deepl_settings`
- Geführte Einrichtungs-Tour starten — Skill `start_guided_tour`
- Zur Seite springen oder eine Karte finden — Skills `navigate_to` (Ziel "settings"),
  `get_page_controls`

### Verwandte Seiten

- Profil (`/workplace/profile`) — persönliche Einstellungen des angemeldeten
  Benutzers, im Gegensatz zur systemweiten Konfiguration hier.
- Gruppen (`/workplace/group`) — die Gruppen selbst werden dort gepflegt; hier wird
  nur ihre Sichtbarkeit pro Benutzer gesteuert.
- Schichtplan (`/workplace/schedule`) — nutzt die hier definierten Planungsregeln,
  Makros, Tabellenfarben und Kalender.
- Absenzen-Kalender (`/workplace/absence`) — zeigt die hier gepflegten Absenztypen
  als Legende und Balkenfarben.
- Dashboard mit Ressourcen-Monitor — speist sich aus Verträgen,
  Planungs-Standardwerten und Planungsregeln dieser Seite.
- Posteingang (`/workplace/inbox`) — nutzt die IMAP- und Spam-Einstellungen.

### Trigger-Phrasen

- "Was sehe ich auf der Einstellungsseite?"
- "Welche Einstellungen gibt es in Klacks?"
- "Wo finde ich die Planungsregeln / den Holistic Harmonizer?"
- "Wo stelle ich den LLM-Anbieter für Klacksy ein?"
- "Where do I set up email and IMAP?"
- "Warum sehe ich das Zahnrad-Icon nicht?" (Admin-Berechtigung)
