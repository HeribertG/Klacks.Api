---
name: explain_page_groups
description: |
  Explains the Groups pages — /workplace/group, the admin page for managing groups
  (teams, locations, organisational units) with two switchable views: a sortable,
  filterable table (with validity filter column) and a hierarchical tree with X|Y
  member badges, expand/collapse/refresh, subgroup creation and drag & drop
  re-parenting. Also covers the group editor at /workplace/edit-group/:id (master
  data with name, validity, payment interval, calendar selection and notes; parent
  structure with current path; participant list with person search and filter
  column), the page /workplace/group-structure (visibility of root groups per
  user), optional group coordinates (latitude/longitude) for geographic
  evaluations, and how the global group selection affects all other workspace
  pages. Use this when the user asks what they see on the Groups page, what the
  cards/columns/tree badges mean, or how to work with it. Supports a level
  parameter: short (purpose only), elements (every element explained), effects
  (data sources, hierarchy model, group scope and visibility effects on other pages).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - groups
  - gruppen
  - gruppe
  - group page
  - gruppenseite
  - gruppenstruktur
  - group structure
  - baumansicht
  - tree view
  - teams
  - gruppenbaum
  - group tree
  - untergruppe
  - subgroup
  - wurzelgruppe
  - root group
  - mitglieder
  - members
  - teilnehmerliste
  - participant list
  - zahlungsintervall
  - payment interval
  - gruppen-koordinaten
  - group coordinates
  - standort
  - location
  - gruppen-sichtbarkeit
  - group visibility
  - hierarchie
  - hierarchy
synonyms:
  de: [was sehe ich hier, erkläre diese seite, was bedeutet diese karte, gruppen, gruppenliste, gruppenbaum, alle gruppen, teilnehmerliste, was bedeutet das badge im gruppenbaum, gruppe anlegen, untergruppe anlegen, gruppe umhängen, mitglied hinzufügen, mitglied entfernen, zahlungsintervall der gruppe, gruppen-koordinaten setzen, wer sieht welche gruppen, sichtbarkeit von gruppen]
  en: [what do I see here, explain this page, what does this card mean, groups, group list, group tree, all groups, participant list, what does the badge in the tree mean, create a group, create a subgroup, move a group, add a member, remove a member, group payment interval, set group coordinates, who sees which groups, group visibility]
  fr: [qu'est-ce que je vois ici, explique cette page, que signifie cette carte, groupes, tous les groupes, structure du groupe, arbre des groupes, liste des participants, créer un groupe, créer un sous-groupe, déplacer un groupe, ajouter un membre, supprimer un membre, intervalle de paiement du groupe, visibilité des groupes]
  it: [cosa vedo qui, spiega questa pagina, cosa significa questa scheda, gruppi, tutti i gruppi, struttura del gruppo, albero dei gruppi, elenco dei partecipanti, creare un gruppo, creare un sottogruppo, spostare un gruppo, aggiungere un membro, rimuovere un membro, intervallo di pagamento del gruppo, visibilità dei gruppi]
---

# Gruppen — die Seiten /workplace/group, /workplace/edit-group und /workplace/group-structure

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

Die Gruppen-Seite verwaltet alle **Gruppen** (Teams, Standorte, Organisationseinheiten) von
Klacks — wahlweise als sortierbare Tabelle oder als hierarchischer Baum mit Untergruppen —
und führt in den Gruppen-Editor mit Stammdaten, Eltern-Struktur und Mitgliederliste.
Gruppen sind das zentrale Ordnungsmittel der App: Die globale Gruppen-Auswahl in der
Kopfleiste filtert Mitarbeitende, Absenzen, Einsatzplanung, Dienste und Verfügbarkeit.
Erreichbar über das Navi-Icon `open-groups` (Tooltip "Alle Gruppen", Alt+6, nur für Admins)
oder die Route `/workplace/group`; der Editor liegt unter `/workplace/edit-group` (neu) bzw.
`/workplace/edit-group/:id` (bestehend), die Gruppen-Sichtbarkeit pro Benutzer unter
`/workplace/group-structure`. Alle drei Routen sind mit AdminGuard geschützt.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand, wirkt auf diese Seite)

- **Suche** (Suchfeld in der Kopfleiste): filtert auf dieser Seite die Gruppen-Tabelle nach
  dem **Gruppennamen** — jedes eingegebene (durch Leerzeichen getrennte) Stichwort muss im
  Namen enthalten sein; die Beschreibung wird von der Kopfleisten-Suche nicht durchsucht.
- **Gruppen-Auswahl** (Dropdown mit Gruppenbaum, Option **Alle Gruppen** (de: "Alle Gruppen",
  en: "All groups", fr: "Tous les groupes", it: "Tutti i gruppi")): wird aus genau diesen
  Gruppen gespeist, filtert aber die Gruppen-Seite selbst NICHT — sie wirkt auf die anderen
  Arbeitsbereich-Seiten (Mitarbeitende, Absenzen, Einsatzplanung, Dienste, Verfügbarkeit),
  siehe Stufe 3.

### Übersichtsseite /workplace/group

Überschrift: **Alle Gruppen** (de: "Alle Gruppen", en: "All groups", fr: "Tous les groupes",
it: "Tutti i gruppi"). Darunter eine von zwei umschaltbaren Ansichten — die Tabellenansicht
ist Standard, die zuletzt gewählte Ansicht wird im Browser gemerkt (LocalStorage
`group-view-mode`).

#### Tabellenansicht — Card **Gruppen** (de: "Gruppen", en: "Groups", fr: "Groupes", it: "Gruppi")

- Kopfzeile: Button `all-group-list-new-button` **+ Gruppe erfassen** (de: "+ Gruppe
  erfassen", en: "+ Create Group", fr: "+ Créer un groupe", it: "+ Crea gruppo") legt eine
  neue Gruppe an und öffnet den Editor; daneben das Baum-Icon `all-group-list-tree-toggle`
  mit Tooltip **Zeige Baumansicht** (de: "Zeige Baumansicht", en: "Show tree view", fr:
  "Afficher la vue en arbre", it: "Mostra vista ad albero") zum Umschalten.
- Tabelle `all-group-list-table`, Spalten sortierbar per Klick auf den Spaltenkopf
  (Sortierpfeil): **NAME** (de: "NAME", en: "NAME", fr: "NOM", it: "NOME") |
  **BESCHREIBUNG** (de: "BESCHREIBUNG", en: "DESCRIPTION", fr: "DESCRIPTION", it:
  "DESCRIZIONE") | **VON** (de: "VON", en: "FROM", fr: "DE", it: "DA") | **BIS** (de: "BIS",
  en: "UNTIL", fr: "JUSQU'À", it: "FINO A") — Von/Bis sind die Gültigkeitsdaten der Gruppe
  (Format dd.MM.yyyy).
- Pro Zeile: Checkbox (Mehrfachauswahl, Header-Checkbox für alle), Aktionen Stift
  (`all-group-list-edit-{i}`, bearbeiten), Kopieren (`all-group-list-copy-{i}`, dupliziert
  die Gruppe als "<Name>-copy") und roter Papierkorb (`all-group-list-delete-{i}`, löschen
  mit Bestätigungsdialog). Unten die Pagination `all-group-list-pagination` mit
  einstellbarer Zeilenzahl.
- Rechts daneben (nur mit Schreibberechtigung sichtbar) eine schmale Filterspalte mit dem
  Dropdown **Gültigkeit** (de: "Gültigkeit", en: "Validity", fr: "Validité", it:
  "Validità"; `all-group-validity-dropdown-toggle`) und den Checkboxen **Aktive**
  (de: "Aktive", en: "Active", fr: "Actifs", it: "Attivi"; `all-group-filter-active`),
  **Einstige** (de: "Einstige", en: "Former", fr: "Ancien", it: "Precedente";
  `all-group-filter-former`) und **Zukünftige** (de: "Zukünftige", en: "Future", fr:
  "Futurs", it: "Futuri"; `all-group-filter-future`) — filtert die Liste nach
  Gültigkeitszeitraum (aktive, abgelaufene, künftige Gruppen).

#### Baumansicht — Card **Gruppenstruktur** (de: "Gruppenstruktur", en: "Group Structure", fr: "Structure du groupe", it: "Struttura del Gruppo")

- Kopfzeile: Icons **Alle ausklappen** (de: "Alle ausklappen", en: "Expand All";
  `tree-group-expand-button`), **Alle einklappen** (de: "Alle einklappen", en: "Collapse
  All"; `tree-group-collapse-button`), **Aktualisieren** (de: "Aktualisieren", en:
  "Refresh"; `tree-group-refresh-button`, berechnet den Baum im Backend neu), Button
  `tree-group-add-root-button` **+ Neue Wurzelgruppe** (de: "+ Neue Wurzelgruppe", en:
  "+ New Root Group", fr: "+ Nouveau groupe racine", it: "+ Nuovo Gruppo Radice") und das
  Grid-Icon `tree-group-grid-toggle` **Zeige Tabellenansicht** (de: "Zeige
  Tabellenansicht", en: "Show grid view").
- Jeder Knoten (`tree-group-node-{id}`) zeigt einen Pfeil zum Auf-/Zuklappen der
  Untergruppen (`tree-node-toggle-{id}`), den Gruppennamen (Tooltip = Beschreibung) und ein
  Zahlen-Badge im Format `X | Y`: **X** = Mitglieder inklusive aller Untergruppen (rekursiv
  summiert), **Y** = direkte Mitglieder nur dieser Gruppe.
- Knoten-Aktionen (mit Schreibrecht): Stift (`tree-node-edit-btn-{id}`, bearbeiten),
  Kopieren (`tree-node-copy-btn-{id}`, dupliziert als "<Name>-copy"), **+**
  (`tree-node-add-btn-{id}`, legt eine Untergruppe direkt unter diesem Knoten an) und
  Papierkorb (`tree-node-delete-btn-{id}`) mit Bestätigungsdialog **"Möchten Sie diese
  Gruppe und alle Untergruppen wirklich löschen?"** — Löschen entfernt die Gruppe MIT allen
  Untergruppen. Ohne Schreibrecht erscheint stattdessen ein Auge (`tree-node-view-btn-{id}`,
  nur ansehen).
- **Drag & Drop:** Ein Knoten lässt sich auf einen anderen ziehen und wird unter den
  Zielknoten umgehängt (Re-Parenting). Beim Ziehen an den oberen/unteren Rand scrollt die
  Ansicht automatisch.
- Die Baumansicht zeigt nur die HEUTE gültigen Gruppen (Gültig-Von ≤ heute ≤ Gültig-Bis);
  einstige oder zukünftige Gruppen sind nur in der Tabellenansicht über den
  Gültigkeits-Filter sichtbar.
- Ist nichts vorhanden: Meldung **Keine Gruppen gefunden** (de: "Keine Gruppen gefunden",
  en: "No groups found", fr: "Aucun groupe trouvé", it: "Nessun gruppo trovato").

### Der Gruppen-Editor — /workplace/edit-group/:id (Überschrift **Gruppe**, en: "Group")

Drei Cards untereinander (Container `edit-group-home-container`):

1. Card **Gruppe Information** (de: "Gruppe Information", en: "Group Information", fr:
   "Informations sur le groupe", it: "Informazioni sul Gruppo"): Pflichtfeld **Name**
   (`edit-group-item-name`), Datums-Picker **Von**/**Bis** (de: "Von"/"Bis", en:
   "From"/"Until"; Validierung: "Gültig-Bis muss nach Gültig-Von liegen"), Dropdown
   **Zahlungsintervall** (de: "Zahlungsintervall", en: "Payment Interval", fr: "Intervalle
   de paiement", it: "Intervallo di pagamento"; `group-payment-interval-dropdown`) mit den
   Optionen **Wöchentlich** (en: "Weekly") / **Zweiwöchentlich** (en: "Biweekly") /
   **Monatlich** (en: "Monthly", Standard) / **Individuell** (en: "Individual"), Dropdown
   **Kalenderauswahl** (de: "Kalenderauswahl", en: "Calendar Selection", fr: "Sélection du
   calendrier", it: "Selezione calendario"; `group-calendar-dropdown`) inkl. Option **Kein
   Kalender** (de: "Kein Kalender", en: "No Calendar", fr: "Aucun calendrier", it: "Nessun
   calendario") sowie **Notizen** (de: "Notizen", en: "Notes") als Rich-Text-Editor
   (`group-description-editor`) — der Text erscheint als BESCHREIBUNG in der Tabelle und
   als Tooltip im Baum.
2. Card **Gruppenstruktur** (de: "Gruppenstruktur", en: "Group Structure"): Auswahl
   **Übergeordnete Gruppe auswählen** (de: "Übergeordnete Gruppe auswählen", en: "Select
   Parent Group", fr: "Sélectionner le groupe parent", it: "Seleziona Gruppo Genitore";
   `edit-group-parent-select`) inkl. Option **Keine übergeordnete Gruppe (Wurzelgruppe)**
   (en: "No parent group (Root group)"). Die eigene Gruppe und alle ihre Untergruppen
   fehlen in der Auswahl — so sind zirkuläre Hierarchien ausgeschlossen. Darunter zeigt
   **Aktueller Pfad** (de: "Aktueller Pfad", en: "Current Path", fr: "Chemin actuel", it:
   "Percorso Attuale") die Position als Breadcrumb (z. B. Wurzel / Region / Team).
3. Card **Teilnehmerliste** (de: "Teilnehmerliste", en: "Participant List", fr: "Liste des
   participants", it: "Elenco dei Partecipanti"): Suchfeld mit Autovervollständigung
   (Platzhalter de: "Name oder ID Nummer eingeben", en: "Enter name or ID number"; Suche ab
   3 Zeichen per Name oder direkt per ID-Nummer) und Button **Übernehmen** (de:
   "Übernehmen", en: "Apply", fr: "Appliquer", it: "Applica") zum Hinzufügen einer Person;
   Mitglieder-Tabelle mit sortierbaren Spalten **NR** | **FIRMA** (en: "COMPANY") |
   **VORNAME** (en: "FIRST NAME") | **NACHNAME** (en: "LAST NAME"), Checkboxen für
   Mehrfachauswahl und rotem Papierkorb zum Entfernen eines Mitglieds.
4. Rechte Filterspalte für die Personensuche: Checkboxen **Frau** (en: "Mrs.") / **Herr**
   (en: "Mr.") / **Juristische Person** (en: "Legal Entity"), Dropdown **Gültigkeit** (en:
   "Validity") mit **Aktive** / **Ehemalige** (en: "Alumni") / **Zukünftige** (Mitgliedschaft),
   **Nationen auswählen** (en: "Select Countries"), **Ktn./Ldr. auswählen** (en: "Select
   States"), **Sichtbarkeit** (en: "Visibility") mit **Eintritt** (en: "Entry") /
   **Austritt** (en: "Resignation") und Von/Bis-Datum sowie **Filter zurücksetzten** (en:
   "Reset Filter").

Geo-Koordinaten (Latitude/Longitude) sind **optionale** Felder am Gruppen-Datensatz für
Standort-Gruppen; sie haben KEIN Eingabefeld auf dieser Seite, sondern werden per
Klacksy-Skill gesetzt (`set_group_location` für eine Gruppe, `geocode_location_groups` für
alle Standort-Gruppen automatisch per Geocoding).

### Seite /workplace/group-structure — Sichtbarkeit von Gruppen pro Benutzer

Überschrift: **Sichtbarkeit von Gruppen pro Benutzer** (de: "Sichtbarkeit von Gruppen pro
Benutzer", en: "Visibility of Groups per User", fr: "Visibilité des groupes par
utilisateur", it: "Visibilità dei gruppi per utente"). Eine Zeile pro Benutzerkonto mit
Name (schreibgeschützt) und einem Zähler-Button (Spalte **Anzahl**, en: "Number") = Anzahl
der zugewiesenen Wurzelgruppen. Klick auf den Zähler öffnet den Dialog **Sichtbare
Rootgruppe** (de: "Sichtbare Rootgruppe", en: "Visible Root Group", fr: "Groupe racine
visible", it: "Gruppo radice visibile") mit einer Checkbox pro Wurzelgruppe. Bei
Admin-Konten ist der Button deaktiviert — Admins sehen immer alles.

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Datenbasis**: Die Tabelle lädt seitenweise über den Gruppen-Filter (Gültigkeit
  aktiv/einstig/zukünftig, Suchstring auf den Namen, Sortierung, Pagination). Die
  Baumansicht lädt die ganze Hierarchie; das Backend berechnet dabei pro Gruppe die
  Mitglieder- und Dienste-Zähler aus den Gruppen-Zuordnungen. Gelöschte Gruppen sind
  im Papierkorb und erscheinen nirgends mehr.
- **Hierarchie**: Gruppen bilden eine echte Baum-Hierarchie (jede Gruppe kann eine
  übergeordnete Gruppe und beliebig viele Untergruppen haben). Umhängen per Drag & Drop oder die Auswahl der übergeordneten Gruppe ordnet ganze
  Teilbäume um; "Aktualisieren" in der Baumansicht berechnet die Struktur im Backend neu.
  Löschen eines Knotens entfernt die Gruppe samt allen Untergruppen.
- **Globaler Gruppen-Scope (wichtigste Wirkung)**: Die hier gepflegten Gruppen speisen die
  Gruppen-Auswahl in der Kopfleiste. Ein Gruppenwechsel dort filtert sofort die Seite, die
  gerade sichtbar ist: **Mitarbeitende** (`/workplace/address`), **Absenzen Kalender**
  (`/workplace/absence`), **Einsatzplanung** (`/workplace/schedule`), **Dienste**
  (`/workplace/shift`) und **Verfügbarkeit** (`/workplace/client-availability`) zeigen dann
  nur noch Daten der gewählten Gruppe (inkl. wählbarer Untergruppen im Baum). In der
  Einsatzplanung übernimmt der Wechsel zusätzlich das **Zahlungsintervall** der Gruppe als
  Periodenraster (Woche/zwei Wochen/Monat) und beendet ein laufendes Analyse-Szenario.
  Die Gruppen-Seite selbst wird vom globalen Scope nicht gefiltert.
- **Zahlungsintervall**: steuert, in welchem Raster die Einsatzplanung für diese Gruppe
  angezeigt und abgerechnet wird (Wöchentlich/Zweiwöchentlich/Monatlich/Individuell).
- **Kalenderauswahl**: verknüpft die Gruppe mit einem Feiertagskalender aus den
  Einstellungen (Option "Kein Kalender" = keiner zugewiesen).
- **Mitgliedschaft**: Die Teilnehmerliste bestimmt, welche Personen beim Gruppen-Filter auf
  den anderen Seiten erscheinen. Eine Person kann in mehreren Gruppen Mitglied sein.
  Gruppen-Zuordnungen verknüpfen daneben auch Dienste mit Gruppen — das geschieht aber in
  der Dienst-Bearbeitung, nicht hier.
- **Sichtbarkeit pro Benutzer** (`/workplace/group-structure`): Die dort gesetzten
  Wurzelgruppen begrenzen für NICHT-Admin-Benutzer serverseitig, welche Daten sie überhaupt
  sehen — im Schichtplan, bei den Mitarbeitenden und in den Dashboard-Widgets (z. B.
  Standorte, Abdeckungs-Statistik) werden nur Daten der sichtbaren Wurzelgruppen geliefert.
  Admins sehen immer alles; die Gruppen-Verwaltungsseiten selbst sind ohnehin Admin-only
  (AdminGuard, Navi-Icon nur für Admins sichtbar).
- **Geo-Koordinaten**: Die optionalen Latitude/Longitude-Werte von Standort-Gruppen sind
  die Grundlage für die geografische Kunden-Zuordnung (Klacksy-Skills
  `propose_customer_grouping` / `apply_customer_grouping`) und Standort-Auswertungen.
- **Berechtigungen**: Ohne Schreibrecht sind Anlegen/Bearbeiten/Kopieren/Löschen
  ausgeblendet bzw. deaktiviert (Felder im Editor schreibgeschützt, im Baum nur das
  Auge-Icon zum Ansehen), die Gültigkeits-Filterspalte der Tabelle erscheint nicht.

### Typische Aufgaben

- Die Edit-Maske ansehen, während sie erklärt wird — Skill `navigate_to`
  (Ziel "new-group" öffnet die leere Gruppen-Maske)

- Gruppen ansehen/durchsuchen — Skills `list_groups`, `list_groups_hierarchical`,
  `search_in_list` (entityType "group", searchQuery = Name)
- Gruppe anlegen / umbenennen / löschen — Skills `create_group`, `update_group`, `delete_group`
- Person in Gruppe aufnehmen/entfernen — Skills `add_client_to_group_by_name`,
  `remove_client_from_group`
- Direkt zu einer Gruppe springen — Skills `search_and_navigate`, `navigate_to` (Ziel "groups")
- Global auf eine Gruppe filtern oder wieder alle zeigen — Skill `select_group`
  (Gruppen-Name oder "all")
- Standort-Koordinaten setzen / Kunden geografisch zuordnen — Skills `set_group_location`,
  `geocode_location_groups`, `propose_customer_grouping`, `apply_customer_grouping`
- Steuern, welche Gruppen ein Benutzer sieht — Skill `set_user_group_scope`

### Verwandte Seiten

- `/workplace/group-structure` — Gruppen-Sichtbarkeit pro Benutzer (oben in Stufe 2 erklärt).
- `/workplace/address` (Mitarbeitende) — dort zeigt die Card "Gruppen" die Mitgliedschaften
  einer einzelnen Person; die Liste wird vom globalen Gruppen-Scope gefiltert.
- `/workplace/schedule` (Einsatzplanung) und `/workplace/shift` (Dienste) — beide werden vom
  globalen Gruppen-Scope gefiltert; das Zahlungsintervall der Gruppe bestimmt das
  Periodenraster der Einsatzplanung.
- Einstellungen — dort werden die Kalender gepflegt, die in der Kalenderauswahl wählbar sind.

### Trigger-Phrasen

- "Was sehe ich auf der Gruppen-Seite?"
- "Was bedeutet das Badge X | Y im Gruppenbaum?"
- "Explain the group structure tree view."
- "Wie hänge ich eine Gruppe unter eine andere?"
- "Wie füge ich jemanden zu einer Gruppe hinzu?"
- "Who sees which groups?" (Sichtbarkeit pro Benutzer)
