---
name: explain_software_updates
description: |
  Explains keeping the installation current: the two release streams, checking automatically versus
  only being told, the nightly window an automatic install is allowed to use, how many database
  copies are kept before an install that changes the schema, and returning to the previous version.
  Covers that only one such operation runs at a time, that everyone signed in is disconnected while
  it happens, and that the window applies to the automatic path only. Use this when the user asks
  how to install a new version, whether it can be undone, or what happens to people working at that
  moment.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  de:
    - software-update
    - neue version installieren
    - rollback
    - wartungsfenster
    - version zurück
  en:
    - software update
    - install new version
    - rollback
    - maintenance window
synonyms:
  de: [software-update, neue version installieren, aktualisierung, rollback, version zurücksetzen, wartungsfenster, beta kanal, wie aktualisiere ich klacks]
  en: [software update, install new version, upgrade, rollback, revert version, maintenance window, beta channel, how do i update]
  fr: [mise à jour logicielle, installer une nouvelle version, retour arrière, fenêtre de maintenance, canal bêta]
  it: [aggiornamento software, installare nuova versione, ripristino, finestra di manutenzione, canale beta]
---

# Software updates — installing a new version and going back

<!-- level:short -->

## Stage 1 — What this is for

The card (de: "Software-Updates", en: "Software Updates") shows the running version, whether a newer
one is offered, and the record of what has been installed. From here an administrator can install a
new version or return to the previous one.

Two release streams exist: **Stable** and **Beta**.

**A prerequisite that decides everything:** the address where new releases are announced is set
during deployment, not on this card. Where it has not been configured, no version is ever offered
and the install button will refuse — the card is then a display of the current version and nothing
more. That is the normal state of a fresh installation.

<!-- level:elements -->

## Stage 2 — The settings

Card anchor: `updates-setting-container`, in the **system** section of the settings page.

- **Automatic updates** (de: "Automatische Updates") — whether a new version installs by itself.
- **Notify only** (de: "Nur benachrichtigen") — check for new versions but never install; only
  report.
- **Channel** (de: "Kanal") — Stable or Beta.
- **Check interval** (de: "Prüfintervall (Stunden)") — how often to look; at least one hour, six by
  default.
- **Backup retention** (de: "Backup-Aufbewahrung") — how many database copies to keep, three by
  default.
- **Maintenance window** (de: "Wartungsfenster Start / Ende") — the time of day an automatic install
  may use. A window running past midnight is allowed. **Careful: the clock used is UTC**, so in
  central Europe the window sits one to two hours away from local time. An unset window means
  "any time".

The card saves on its own; there is no save button.

**The record** below shows what was done: the kind of operation, the target version, its outcome and
who asked for it. Note that it also lists operations of the self-hosted speech recognition, which
shares the same record.

<!-- level:effects -->

## Stage 3 — What actually happens

**Only one such operation runs at a time.** While one is in progress, no other can be started —
including an installation of the self-hosted speech recognition, which will block an update and be
blocked by one.

**Installing** proceeds as: verify the downloaded release, take a database copy **only when the
release changes the database structure**, activate the new version, and check that it comes up
healthy. If it does not, the previous version is **restored automatically**.

**Going back** stops the current version, restores the database copy if one was taken, activates the
previous version and checks its health again.

**Everyone signed in is disconnected.** The application is replaced while this runs; there is no
warning to those working, no read-only phase and no graceful drain. That is what the maintenance
window is for.

**And the window only applies to the automatic path.** Pressing install by hand starts immediately,
whatever the window says. Anybody expecting the window to hold back a manual install will be
surprised.

Only an administrator can reach any of this.

## Related skills

- `get_update_status`, `get_update_config`, `update_update_config`

## Trigger phrases

- "Wie installiere ich die neue Version?"
- "Can an update be undone?"
- "Was passiert mit den Leuten, die gerade arbeiten?"
- "Es wird keine neue Version angezeigt."
- "Läuft das Update auch ausserhalb des Wartungsfensters?"
