---
name: explain_oauth_connections
---

# Connecting an AI tool by OAuth: only trusted tools, read-only unless you choose otherwise

An AI tool such as Claude or ChatGPT can connect to Klacks by itself through the sign-in page (OAuth). Klacks only hands the connection to tools it trusts, and the person who connects chooses what the tool may do.

<!-- level:short -->

## What it is and who may do it

The tool asks for a connection, Klacks shows a consent page, the user signs in with their own account and confirms, and the tool receives an access token that works like the user's own sign-in for tool use. Only a Supervisor or an Admin can connect a tool; an account without a role (planner) cannot, and any token it still holds stops working. The access token is valid for 30 days (24 hours on the public demo) and shows up in the user's token list under the tool's name; the user can revoke it there at any time.

## What the consent page shows

The page is titled "Connect an external AI tool to Klacks". It shows the tool's name and, large beneath it, the address the access is handed to: always taken from the list of trusted targets, never from a name the tool announces itself. Below that: the tool acts in your name with the rights you choose, continue only if you just started the connection yourself. For a program on this computer a warning line adds the host and port and says that anyone who can run programs on this computer could receive the access. Then two choices, **Read only** (preselected) and **Read and change**, the sentence how long the connection is valid, and the sign-in with email and password, with the buttons to allow or deny. The page speaks the language of the browser (Accept-Language), else the installation language, else English.

<!-- level:elements -->

## Which tools connect without an administrator

- **Claude** (claude.ai and claude.com) and **ChatGPT** connect directly.
- **Programs on the same computer** (for example Claude Code, Cursor or VS Code on the desktop) connect directly while the administrator keeps the local programs switch on (it is on by default). They are shown as "Program on this computer" with the port.
- **Every other tool** is refused until an administrator adds its address as an own target in the card "OAuth connections of AI tools" (Settings, section "System", expert mode). Once added, every Supervisor and Admin can use it. Only Admins add targets.

The name on the consent page and in the token list always comes from this list of trusted targets, never from a name the tool announces itself. The address the code is sent to is shown next to the name; connect only if you just started the connection yourself.

## Why a tool is refused

The message names the refused address, for example "Redirect target 'https://example.com' is not trusted". Typical causes: the tool is not on the list and has no own target yet; it is a local program while the local programs switch is off; or the administrator switched OAuth connections off completely (mode "off"). A tool that sends several return addresses is still connected through the trusted ones; only when none is trusted is it refused. Ask an administrator to add the exact address named in the message.

<!-- level:effects -->

## Read only or also change data

The consent page preselects read only: the tool can look things up (plans, employees and other data) but not change anything. Read and change must be chosen on purpose, and the option only appears while the administrator keeps the write switch on (on by default); with the switch off the page offers read only. If the switch is turned off after the user confirmed but before the tool collected its token, the connection is refused and has to be started again. Tokens already issued keep the mode they were granted.

## What the administrator controls

Where: Settings (Admin only), expert mode switched on at the top, section "System", card "OAuth connections of AI tools". Supervisors and planners do not see the card. It holds the connections mode ("Trusted tools only" by default, or "Off"), the switch for programs on this computer, the write switch and the list of own trusted targets. Mode "off" blocks new connections and also stops every access token that was issued through OAuth, including existing ones, until the mode is switched back. On a terminal server, switch local programs off: another user on the same machine could intercept the connection of a local program. On the public demo an OAuth token lives only 24 hours.

## Trigger phrases

- "Why is my AI tool refused when it tries to connect?"
- "Wie verbinde ich Claude oder ChatGPT mit Klacks?"
- "Wie gebe ich ein eigenes KI-Werkzeug frei?"
- "Can the tool change my data after connecting?"
