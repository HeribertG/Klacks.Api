---
name: explain_office_pc_installation
---

# Installing Klacks: Klacks Store or Microsoft Store

Klacks can be obtained in two places. The Klacks Store offers every way of running Klacks; the Microsoft Store offers only the office PC.

<!-- level:short -->

## The two places

- **Klacks Store** (klacks-software.ch/store): every way of running Klacks. The office PC (Windows, without Docker), the server with Docker and Linux. The country and the industry are chosen there before the download, and the country and industry packages are only available there.
- **Microsoft Store** (entry "Klacks", apps.microsoft.com/store/detail/XPFFW8HDX0KX26): only the office PC for Windows. It is the same setup, but it installs without asking any questions and applies the country set in the Windows regional format of the PC. If Klacks has no profile for that country, it installs without a country profile.

<!-- level:elements -->

## Which way fits

- A single Windows PC in the office, country of the PC is the country of use: the Microsoft Store is the simplest way.
- A server, Docker, Linux, or a different country or an industry package: the Klacks Store. The steps on Linux: `explain_linux_installation`.
- Unsure: the Klacks Store, because it covers every case.

Klacks is installed only once per PC. A later setup of a different version does not install over it; newer versions come through Settings, card (de: "Software-Updates", en: "Software Updates"), see `explain_software_updates`.

<!-- level:effects -->

## Updates and uninstalling

**Updates never come from the Microsoft Store.** For apps of this kind the Microsoft Store does not distribute updates; Klacks updates itself through the card (de: "Software-Updates", en: "Software Updates"), whichever store it came from. On a PC without data from an earlier installation, a new office-PC installation starts with automatic updates switched on. Installing from the Microsoft Store while another version of Klacks is on the PC does not update it: the setup refuses, whether the installed version is older or newer. Running the setup of the version already installed repairs the installation.

**Uninstalling keeps the data.** Klacks is removed under the installed apps of Windows. The uninstaller asks whether the database, the settings and the backups should also be deleted; the preselected answer is No, and a silent uninstall also keeps them. The data stays in the Klacks folder under ProgramData, so a later installation can take it over.

## Trigger phrases

- "Gibt es Klacks im Microsoft Store?"
- "Where do I download Klacks?"
- "Microsoft Store oder Klacks Store, was soll ich nehmen?"
- "Bleiben meine Daten nach der Deinstallation erhalten?"
