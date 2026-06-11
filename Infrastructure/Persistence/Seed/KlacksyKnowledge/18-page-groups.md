---
name: explain_page_groups
description: |
  Explains the Groups page (/workplace/group) — the admin page for managing groups
  (teams, locations, organisational units): a sortable, filterable list view, a
  hierarchical tree view with drag & drop re-parenting and member counts, and the
  group editor with master data (name, validity, payment interval, calendar),
  parent structure and participant list. Use this when the user asks what they see
  on the Groups page, what the cards/columns/tree badges mean, or how to work with it.
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
synonyms:
  de: [was sehe ich hier, erkläre diese seite, was bedeutet diese karte, gruppen, gruppenliste, gruppenbaum, alle gruppen, teilnehmerliste]
  en: [what do I see here, explain this page, what does this card mean, groups, group list, group tree, all groups]
  fr: [qu'est-ce que je vois ici, explique cette page, que signifie cette carte, groupes, tous les groupes, structure du groupe, arbre des groupes]
  it: [cosa vedo qui, spiega questa pagina, cosa significa questa scheda, gruppi, tutti i gruppi, struttura del gruppo, albero dei gruppi]
---

# Seite „Gruppen" — /workplace/group

## Zweck (1 Satz)

Die Gruppen-Seite verwaltet alle **Gruppen** (Teams, Standorte, Organisationseinheiten) von
Klacks — als Tabelle oder als hierarchischer Baum — und führt in den Gruppen-Editor mit
Stammdaten, Eltern-Struktur und Mitgliederliste.

**Hinkommen:** Navi-Icon `open-groups` in der linken Leiste (Tooltip „Alle Gruppen", Shortcut
Alt+6, nur für Admins sichtbar; Route ist mit AdminGuard geschützt). Route: `/workplace/group`.
Der Editor liegt unter `/workplace/edit-group` (neu) bzw. `/workplace/edit-group/:id` (bestehend).

## Aufbau der Übersichtsseite

Überschrift: **Alle Gruppen** (de: „Alle Gruppen", en: "All groups", fr: "Tous les groupes",
it: "Tutti i gruppi"). Darunter eine von zwei umschaltbaren Ansichten (Tabellen-Ansicht ist
Standard, die Wahl wird gemerkt):

### 1. Card **Gruppen** (de: „Gruppen", en: "Groups", fr: "Groupes", it: "Gruppi") — Tabellenansicht

- Kopfzeile: Button **+ Gruppe erfassen** (en: "+ Create Group", fr: "+ Créer un groupe",
  it: "+ Crea gruppo") legt eine neue Gruppe an und öffnet den Editor; daneben ein Baum-Icon
  **Zeige Baumansicht** (en: "Show tree view") zum Umschalten.
- Spalten (sortierbar per Klick auf den Spaltenkopf): **NAME** | **BESCHREIBUNG**
  (en: "DESCRIPTION") | **VON** (en: "FROM") | **BIS** (en: "UNTIL") — Von/Bis sind die
  Gültigkeitsdaten der Gruppe (dd.MM.yyyy).
- Pro Zeile: Checkbox (Mehrfachauswahl, Header-Checkbox für alle), Aktionen Stift (bearbeiten),
  Kopieren (dupliziert die Gruppe als „<Name>-copy") und roter Papierkorb (löschen mit
  Bestätigungsdialog). Unten eine Pagination mit einstellbarer Zeilenzahl.
- Rechts daneben (nur mit Berechtigung) eine schmale Filterspalte mit Dropdown **Gültigkeit**
  (de: „Gültigkeit", en: "Validity", fr: "Validité", it: "Validità") und den Checkboxen
  **Aktive** / **Einstige** / **Zukünftige** — filtert die Liste nach Gültigkeitszeitraum.

### 2. Card **Gruppenstruktur** (de: „Gruppenstruktur", en: "Group Structure", fr: "Structure du groupe", it: "Struttura del Gruppo") — Baumansicht

- Kopfzeile: Icons **Alle ausklappen** / **Alle einklappen** / **Aktualisieren**, Button
  **+ Neue Wurzelgruppe** (en: "+ New Root Group") und Grid-Icon **Zeige Tabellenansicht**.
- Jeder Knoten zeigt Pfeil (Untergruppen auf-/zuklappen), den Gruppennamen (Tooltip =
  Beschreibung) und ein Zahlen-Badge im Format `X | Y`: **X** = Mitglieder inklusive aller
  Untergruppen (rekursiv summiert), **Y** = direkte Mitglieder dieser Gruppe.
- Knoten-Aktionen: bearbeiten (Stift), kopieren, **+** (Untergruppe direkt unter diesem Knoten
  anlegen), löschen. Ohne Schreibrecht erscheint stattdessen ein Auge (nur ansehen).
- **Drag & Drop:** Ein Knoten kann auf einen anderen gezogen werden und wird dadurch unter den
  Zielknoten umgehängt (Re-Parenting der Hierarchie).
- Ist nichts vorhanden: Meldung **Keine Gruppen gefunden** (en: "No groups found").

## Der Gruppen-Editor (Überschrift **Gruppe**, en: "Group")

1. Card **Gruppe Information** (de: „Gruppe Information", en: "Group Information",
   fr: "Informations sur le groupe", it: "Informazioni sul Gruppo"): Felder **Name**,
   **Von**/**Bis** (Gültigkeit), Dropdown **Zahlungsintervall** (en: "Payment Interval"),
   Dropdown **Kalenderauswahl** (en: "Calendar Selection", inkl. Option **Kein Kalender**)
   sowie **Notizen** (en: "Notes") als Rich-Text-Beschreibung.
2. Card **Gruppenstruktur** (en: "Group Structure"): Auswahl **Übergeordnete Gruppe auswählen**
   (en: "Select Parent Group") inkl. Option **Keine übergeordnete Gruppe (Wurzelgruppe)**;
   darunter zeigt **Aktueller Pfad** (en: "Current Path") die Position als Breadcrumb
   (z. B. Wurzel / Region / Team).
3. Card **Teilnehmerliste** (de: „Teilnehmerliste", en: "Participant List", fr: "Liste des
   participants", it: "Elenco dei Partecipanti"): Suchfeld mit Autovervollständigung und Button
   **Übernehmen** (en: "Apply") zum Hinzufügen einer Person; Mitglieder-Tabelle mit Spalten
   **NR** | **FIRMA** | **VORNAME** | **NACHNAME** und Papierkorb zum Entfernen.
4. Rechte Filterspalte für die Personensuche: Checkboxen **Frau** / **Herr** /
   **Juristische Person**, Dropdown **Gültigkeit** (Aktive/Ehemalige/Zukünftige Mitgliedschaft),
   **Nationen auswählen**, **Ktn./Ldr. auswählen**, **Sichtbarkeit** (Eintritt/Austritt mit
   Von/Bis-Datum) und **Filter zurücksetzten**.

## Datenmodell-Hinweise

- Gruppen bilden eine echte Hierarchie (Nested Set: lft/rgt/depth, parent/root) und tragen
  Zähler (clientsCount, shiftsCount, customersCount, employeesCount, externEmpsCount).
- Geo-Koordinaten (Latitude/Longitude) sind **optionale** Felder am Gruppen-Datensatz für
  Standort-Gruppen; sie werden nicht auf dieser Seite gepflegt, sondern per Klacksy-Skill.

## Typische Aufgaben + passende Klacksy-Skills

- Gruppen ansehen/durchsuchen → `list_groups`, `list_groups_hierarchical`, `search_in_list`
- Gruppe anlegen / umbenennen / löschen → `create_group`, `update_group`, `delete_group`
- Person in Gruppe aufnehmen/entfernen → `add_client_to_group_by_name`, `remove_client_from_group`
- Direkt zu einer Gruppe springen → `search_and_navigate`, `navigate_to`
- Standort-Koordinaten setzen / Kunden geografisch zuordnen → `set_group_location`,
  `geocode_location_groups`, `propose_customer_grouping`, `apply_customer_grouping`
- Steuern, welche Gruppen ein Benutzer sieht → `set_user_group_scope`

## Verwandte Seiten

- `/workplace/group-structure` — Gruppen-Sichtbarkeit pro Benutzer (Group Scope, Settings).
- `/workplace/address` (Mitarbeitende) — dort zeigt die Card „Gruppen" die Mitgliedschaften
  einer einzelnen Person.

## Trigger-Phrasen

- „Was sehe ich auf der Gruppen-Seite?"
- „Was bedeutet das Badge X | Y im Gruppenbaum?"
- "Explain the group structure tree view."
- „Wie hänge ich eine Gruppe unter eine andere?"
