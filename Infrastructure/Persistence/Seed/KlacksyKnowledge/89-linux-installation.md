---
name: explain_linux_installation
---

# Installing Klacks on Linux (Docker)

On Linux, Klacks runs as a Docker installation: a ZIP from the Klacks Store with an installer script that sets everything up.

<!-- level:short -->

## What it is

- The Linux package comes only from the Klacks Store (klacks-software.ch/store), on the page of the country; the country profile is already inside.
- Requirements: about 8 GB RAM (Docker must see at least 7 GiB, otherwise a first installation stops), free ports 80 and 443, internet access for the Docker images and updates.
- Mac is not supported. On Windows use the office-PC setup or the Docker installer for Windows, see `explain_office_pc_installation`.

<!-- level:elements -->

## Steps

1. Download the ZIP and unpack it into a permanent folder, for example `~/klacks`, not into Downloads: automatic updates keep working with this folder, so it must stay where it is.
2. Open a terminal in that folder and run `sudo bash ./install-<cc>.sh` (`<cc>` is the country code in the file name, e.g. `install-ch.sh`).
3. If Docker is missing, the installer asks whether to install it with Docker's official script (Ubuntu, Debian, Fedora, CentOS, RHEL). The default answer is No; other distributions need Docker Engine with the Compose plugin installed by hand.
4. At the end the installer prints a **setup code**. Open `https://localhost` (from another PC: `https://` and the server name), confirm the browser's certificate warning (the certificate is self-signed) and create your own administrator account with the setup code.

<!-- level:effects -->

## Updates, repair and what to keep

- **Updates come by themselves** through the updater of the installation; they are managed in Settings, card (de: "Software-Updates", en: "Software Updates"), see `explain_software_updates`.
- **Running the installer again repairs, it does not update**: it keeps the version recorded in the installation and only pulls and restarts it. Run it with `sudo` again.
- The file `.env` in the folder holds the database password, the keys and the setup code. Keep a copy of it safe: without it stored passwords and API keys cannot be recovered.
- Deleting or moving the folder breaks the automatic updates.
- To reach Klacks from other PCs, set the server name on the first installation: `sudo SERVER_NAME=<name> bash ./install-<cc>.sh`. Without it the certificate is issued for `localhost`.
- On a machine with little RAM, set `KLACKS_API_MEMORY_LIMIT` together with `KLACKS_API_IDLE_UNLOAD_MINUTES` (only both, the installer refuses one alone), for example `sudo KLACKS_API_MEMORY_LIMIT=2560M KLACKS_API_IDLE_UNLOAD_MINUTES=30 bash ./install-<cc>.sh`.

## Trigger phrases

- "Wie installiere ich Klacks auf Linux?"
- "How do I install Klacks on a server with Docker?"
- "Comment installer Klacks sous Linux ?"
- "Läuft Klacks auf dem Mac?"
