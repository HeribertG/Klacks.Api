---
name: explain_plugin_architecture
description: |
  Explains Klacks' backend plugin architecture for developers: the four-layer split into
  Klacks.Plugin.Contracts (shared interfaces), a plugin assembly like Klacks.Plugin.Messaging, the
  Klacks.Api host, and Angular plugin libraries. Covers the dependency direction -- a plugin
  references only the contracts, never the host -- how IPluginRegistrar is registered manually in
  Program.cs, and the rough steps to build a brand-new plugin assembly from scratch.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - plugin-architektur
    - pluginregistrar
    - eigenes plugin bauen
    - plugin contracts
  en:
    - plugin architecture
    - pluginregistrar
    - build a plugin
    - plugin contracts
synonyms:
  de: [plugin-architektur, pluginregistrar, plugin contracts, eigenes plugin bauen, neues plugin erstellen, plugin assembly, abhängigkeitsrichtung plugin]
  en: [plugin architecture, pluginregistrar, plugin contracts, build a new plugin, plugin assembly, dependency direction]
  fr: [architecture de plugin, contrats de plugin, créer un nouveau plugin, registrar de plugin]
  it: [architettura del plugin, contratti del plugin, creare un nuovo plugin, registrar del plugin]
---

# Plugin-Architektur — Backend-Erweiterungen für Klacks

## Kern-Idee (1 Satz)

Klacks-Plugins sind eigenständige Backend-Assemblies (und Angular-Libraries im Frontend), die
ausschliesslich über definierte **Contracts** mit dem Host (`Klacks.Api`) kommunizieren — nie
direkt gegeneinander verdrahtet. Das Messaging-Plugin (`Klacks.Plugin.Messaging`) ist die
Referenzimplementierung.

## Die 4 Schichten

| Schicht | Projekt | Zweck |
|---|---|---|
| Contracts | `Klacks.Plugin.Contracts` | Reine Interfaces, keine Abhängigkeiten |
| Plugin | z.B. `Klacks.Plugin.Messaging` | Eigenes Assembly, referenziert NUR Contracts |
| Host | `Klacks.Api` | Bridges + Registrierungs-Infrastruktur |
| Frontend | `Klacks.Ui/projects/klacks-plugin-*` | Angular-Libraries |

## Abhängigkeitsrichtung

- Ein Plugin referenziert **nur** `Klacks.Plugin.Contracts`, nie den Host direkt.
- Der Host referenziert Contracts + das Plugin-Assembly (für die Registrierung).
- `Klacks.Plugin.Contracts` selbst hat keine Abhängigkeiten.

## Kern-Interfaces in Klacks.Plugin.Contracts

- `IPluginRegistrar` — `RegisterServices`, `ConfigureDbModel`, `GetControllerAssemblies`,
  `GetSkillAssemblies`.
- `IPluginEventBus` — SignalR-Events an einen Nutzer oder als Broadcast.
- `IPluginUnitOfWork` — DB-Transaktionen (`CompleteAsync`).
- `IPluginSettingsReader` — Settings lesen.
- `IPluginStateChecker` — ist das Plugin gerade aktiv?
- `IPluginOperationalCheck` — Konfigurations-Check, ob das Plugin betriebsbereit ist.

## Registrierung — manuell, kein Auto-Scan für die Plugins selbst

In `Klacks.Api/Program.cs`, **vor** `AddApplicationServices()`:

```csharp
RegisterPlugin(new MessagingPluginRegistrar());
```

Das trägt den Registrar in eine statische `PluginRegistrars`-Liste ein. Diese Registrierung läuft
**nicht** über Assembly-Scanning — jedes Plugin braucht diesen expliziten Aufruf im Code. Scanning
kommt erst nachgelagert ins Spiel: für Skills (`GetSkillAssemblies`) und für Feature-Manifeste,
nicht für die Plugin-Registrierung selbst.

## Unterschied zum Feature-Plugin-Manifest-System

Das ist eine andere, nachgelagerte Schicht: `Plugins/Features/{name}/manifest.json` zusammen mit
den Settings `FEATURE_PLUGIN_{NAME}` (installiert) / `FEATURE_PLUGIN_{NAME}_ENABLED` (aktiviert)
und `FeaturePluginService.DiscoverPlugins()` regeln, ob ein **bereits gebautes** Plugin zur
Laufzeit ein- oder ausgeschaltet ist — das ist der Admin-Toggle für Endnutzer. Diese Schicht
ersetzt nicht den Bauprozess oben: ein Plugin muss zuerst als Assembly mit Registrar existieren
und in `Program.cs` eingetragen sein, bevor ein Manifest überhaupt etwas zum Ein-/Ausschalten hat.

## Neues Plugin erstellen — grober Ablauf

1. Neues Projekt `Klacks.Plugin.{Name}` anlegen, referenziert **nur** `Klacks.Plugin.Contracts`.
2. `{Name}PluginRegistrar : IPluginRegistrar` implementieren (Services, DB-Model, Controller- und
   Skill-Assemblies).
3. Controller mit `[RequireFeaturePlugin("{name}")]` versehen, Skills mit
   `[SkillImplementation("...")]`.
4. In `Klacks.Api/Program.cs`: `RegisterPlugin(new {Name}PluginRegistrar())` eintragen, vor
   `AddApplicationServices()`.
5. `Plugins/Features/{name}/manifest.json` für den Laufzeit-Toggle anlegen.
6. Frontend: `ng generate library klacks-plugin-{name}`, die Library importiert ausschliesslich
   von `klacks-plugin-contracts`, nie von `src/app/`.

**Wichtig:** Dieser Bauprozess selbst — ein neues Projekt anlegen, den Registrar-Code schreiben,
`Program.cs` editieren — ist **nicht** durch Klacksy automatisierbar. Es gibt dafür keinen
Act-Skill; dieser Skill liefert reines Wissen für Entwickler und den Owner, keine Ausführung.

## Related skills

- `list_feature_plugin`, `install_feature_plugin`, `enable_feature_plugin`,
  `disable_feature_plugin` — schalten ein bereits fertig gebautes Plugin zur Laufzeit ein oder aus;
  sie bauen keinen neuen Plugin-Code.

## Trigger phrases

- "Wie baue ich ein eigenes Plugin für Klacks?"
- "Was macht IPluginRegistrar?"
- "Wie unterscheidet sich das Feature-Plugin-Manifest vom Plugin-Bauprozess?"
- "How do I write a new backend plugin?"
- "What's the dependency direction between a plugin and the host?"
- "Kannst du mir automatisch ein neues Plugin bauen?"
