---
name: explain_page_profile
description: |
  Explains the My Profile page (Mein Profil) at /workplace/profile — the personal page of
  the logged-in user with four cards: profile picture (drag & drop upload or click, instant
  delete; the picture also becomes the sidebar profile icon), change my login data
  (read-only username, current password with show/hide toggle, new password with live
  strength indicator, repeat field locked until the password is strong, saved via the
  footer save bar), custom settings (theme with 7 color schemes and the display language,
  both applied instantly without saving) and the microphone & voice recognition test
  (device picker shared with the Klacksy voice input, four-step diagnostics, 5-second
  record/playback with level meter and automatic transcription). Use this when the user
  asks what they see on the profile page, how to change their password, picture, theme or
  language, or how to test the microphone. Supports a level parameter: short (purpose
  only), elements (every element explained), effects (data sources, what saving the
  password does, and how the page interacts with the sidebar, language packs and the
  Klacksy voice input).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - my profile
  - mein profil
  - profile page
  - profilseite
  - profilbild
  - profile picture
  - avatar
  - passwort ändern
  - change password
  - logindaten ändern
  - login data
  - neues passwort
  - new password
  - farbschema
  - theme
  - dark mode
  - sprache ändern
  - change language
  - benutzerdefinierte einstellungen
  - custom settings
  - mikrofon test
  - microphone test
  - mikrofon auswählen
  - spracherkennung
  - speech recognition
  - benutzername
  - username
synonyms:
  de: [was sehe ich hier, erkläre diese seite, mein profil, profilseite, profilbild hochladen, profilbild löschen, passwort ändern, logindaten ändern, neues passwort, warum ist das wiederholen-feld gesperrt, farbschema ändern, dark mode einschalten, sprache umstellen, mikrofon testen, mikrofon auswählen, spracherkennung testen, diagnose starten]
  en: [what do i see here, explain this page, my profile, profile page, upload profile picture, remove profile picture, change my password, change login data, new password, why is the repeat field disabled, change theme, enable dark mode, switch language, test my microphone, select microphone, test speech recognition, run diagnostics]
  fr: [que vois-je ici, explique cette page, mon profil, photo de profil, changer le mot de passe, nouveau mot de passe, changer le thème, mode sombre, changer la langue, tester le microphone, sélectionner le microphone, reconnaissance vocale]
  it: [cosa vedo qui, spiega questa pagina, il mio profilo, immagine del profilo, cambiare la password, nuova password, cambiare il tema, modalità scura, cambiare la lingua, testare il microfono, selezionare il microfono, riconoscimento vocale]
---

# Mein Profil — die Seite /workplace/profile

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

**Mein Profil** (de: "Mein Profil", en: "My Profile", fr: "Mon profil", it: "Il mio
profilo") ist die persönliche Seite des angemeldeten Benutzers — sie zeigt immer die
eigenen Daten, unabhängig von Gruppen-Auswahl oder Suche. Vier Karten: **Profilbild**
(hochladen/entfernen), **Meine Logindaten ändern** (Passwort wechseln),
**benutzerdefinierte Einstellungen** (Farbschema und Sprache) und **Mikrofon &
Spracherkennung Test**. Erreichbar für ALLE angemeldeten Benutzer (kein Admin-Recht
nötig) über das unterste Sidebar-Icon `open-profile` (Benutzer-Symbol bzw. das eigene
Profilbild, Alt+7) oder die Route `/workplace/profile`.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand)

- **Suche**: auf dieser Seite AUSGEBLENDET — es gibt hier nichts zu durchsuchen, die
  Seite zeigt nur die eigenen Daten.
- **Gruppen-Auswahl**: hat auf dieser Seite KEINE Wirkung — der globale Gruppen-Scope
  filtert nur Daten-Seiten (Mitarbeiter, Absenzen, Einsatzplan, Dienste, Verfügbarkeit),
  nicht die persönliche Profilseite.

Die Seite trägt die Überschrift `profile-home-headline` und ordnet die vier Karten in
zwei Reihen an: oben `profile-home-upper-row` (Profilbild + Logindaten), unten
`profile-home-lower-row` (benutzerdefinierte Einstellungen + Mikrofon-Test).

### Karte Profilbild (oben links)

Titel **Profilbild** (de: "Profilbild", en: "Profile Picture", fr: "Photo de profil",
it: "Immagine del profilo").

- **Ohne Bild**: Upload-Fläche `profile-picture-upload-area` mit dem Hinweis (de:
  "Image-Datei hierhin ziehen oder klicken um eine Datei hochzuladen.", en: "Drag image
  file here or click to upload a file.", fr: "Glissez le fichier image ici ou cliquez
  pour télécharger un fichier.", it: "Trascina qui il file immagine o fai clic per
  caricare un file."). Bild per **Drag & Drop** auf die Fläche ziehen oder klicken — das
  öffnet die Dateiauswahl `profile-picture-file-input` (akzeptiert JPEG, PNG, GIF und
  ICO). Der Upload startet sofort, ohne Speichern-Knopf; die Datei wird unter dem Namen
  `{Benutzer-ID}profile.{Endung}` abgelegt.
- **Mit Bild**: Anzeige des Bildes (`profile-picture-image`) und darunter der Link
  `profile-picture-delete-button` **Profilbild entfernen** (de: "Profilbild entfernen",
  en: "Remove Profile Picture", fr: "Supprimer la photo de profil", it: "Rimuovi
  immagine del profilo") — löscht das Bild sofort.

### Karte Meine Logindaten ändern (oben rechts)

Titel (de: "Meine Logindaten ändern", en: "Change My Login Data", fr: "Modifier mes
données de connexion", it: "Modifica i miei dati di accesso").

- **Benutzername** (de: "Benutzername", en: "Username", fr: "Nom d'utilisateur", it:
  "Nome utente") — Feld `profile-data-edit-username`, NUR LESBAR; kommt aus dem
  Login-Token und ist hier nicht änderbar.
- **Aktuelles Passwort** (de: "Aktuelles Passwort", en: "Current Password", fr: "Mot de
  passe actuel", it: "Password attuale") — Feld `profile-data-edit-old-password` mit
  Auge-Symbol zum Anzeigen/Verbergen der Eingabe.
- **Neues Passwort** (de: "Neues Passwort", en: "New Password", fr: "Nouveau mot de
  passe", it: "Nuova password") — Feld `profile-data-edit-new-password` mit
  Live-Stärkeanzeige `profile-data-edit-password-strength` bei jedem Tastendruck.
  Regeln: unter 8 Zeichen = "zu kurz"; häufige Muster (z. B. passw…, 12345…, qwert…) =
  zu unsicher; sonst zählt die Anzahl Zeichenklassen (Kleinbuchstaben, Großbuchstaben,
  Ziffern, Sonderzeichen) — erst ab **3 von 4 Klassen** gilt das Passwort als "sicher"
  (grün), alles darunter bleibt rot.
- **Neues Passwort wiederholen** (de: "Neues Passwort wiederholen", en: "Repeat New
  Password", fr: "Répétez le nouveau mot de passe", it: "Ripeti nuova password") — Feld
  `profile-data-edit-new-password-repeat`, GESPERRT (deaktiviert), bis das neue Passwort
  die Stufe "sicher" erreicht. Das ist die häufigste Frage auf dieser Seite: das
  Wiederholen-Feld bleibt absichtlich gesperrt, solange das neue Passwort zu schwach ist.
- **Speichern über die Fußleiste**: erst wenn aktuelles Passwort ausgefüllt, neues
  Passwort "sicher" UND Wiederholung identisch ist, erscheinen unten **Eingaben
  speichern** (de: "Eingaben speichern", en: "Save entries", fr: "Enregistrer les
  entrées", it: "Salva le voci") und **Zurücksetzen** (de: "Zurücksetzen", en: "Reset",
  fr: "Réinitialiser", it: "Reset"). Einen "Speichern und schließen"-Knopf gibt es auf
  dieser Seite bewusst nicht. Nach dem Speichern werden die Passwortfelder geleert.

### Karte benutzerdefinierte Einstellungen (unten links)

Titel (de: "benutzerdefinierte Einstellungen", en: "Custom Settings", fr: "Paramètres
personnalisés", it: "Impostazioni personalizzate"). Beide Auswahlen wirken SOFORT, ohne
Speichern-Knopf.

- **Farbschema** (de: "Farbschema", en: "Theme", fr: "Thème", it: "Tema") — Auswahl
  `profile-custom-setting-theme-select` mit 7 Schemata: Light, Dark, High Contrast,
  Blue, Warm, OLED Dark, Dimmed. Der Wechsel färbt die ganze App sofort um.
- **Ausgewählte Sprache** (de: "Ausgewählte Sprache:", en: "Selected language:", fr:
  "Langue sélectionnée :", it: "Lingua selezionata:") — Auswahl
  `profile-custom-setting-language-select`; listet alle verfügbaren Sprachen (die vier
  Kernsprachen Deutsch/Englisch/Französisch/Italienisch plus alle installierten
  Sprachpakete). Der Wechsel übersetzt alle Beschriftungen sofort und stellt auch das
  Zahlen- und Datumsformat (Locale) um.

### Karte Mikrofon & Spracherkennung Test (unten rechts)

Titel (de: "Mikrofon & Spracherkennung Test", en: "Microphone & Voice Recognition
Test", fr: "Test Microphone & Reconnaissance Vocale", it: "Test Microfono &
Riconoscimento Vocale") — prüft die Voraussetzungen für die Sprachsteuerung.

- **Mikrofon auswählen** (de: "Mikrofon auswählen", en: "Select microphone", fr:
  "Sélectionner le microphone", it: "Seleziona microfono") — Auswahl
  `mic-device-picker` mit der Option **Systemstandard** (de: "Systemstandard", en:
  "System default") und allen erkannten Eingabegeräten; daneben ein Refresh-Knopf
  **Geräte aktualisieren**. Fehlen die Gerätenamen, erscheint der Hinweis, einmal die
  Mikrofon-Berechtigung zu erteilen.
- **Diagnose starten** (de: "Diagnose starten", en: "Run Diagnostics", fr: "Lancer le
  diagnostic", it: "Avvia diagnostica") — führt vier Prüfschritte nacheinander aus und
  zeigt sie unter **Diagnose-Ergebnisse** mit Häkchen (grün) oder Kreuz (rot):
  1. **Sichere Verbindung (HTTPS)** — ohne HTTPS/localhost bricht der Test hier ab.
  2. **Media Devices API** — Browser-Unterstützung für Audiogeräte.
  3. **Mikrofon-Zugriff** — fordert die Berechtigung an; bei Ablehnung Fehlermeldung.
  4. **Spracherkennungs-API** — meldet, ob die Browser-Spracherkennung (Web Speech API)
     oder der Whisper-Fallback verwendet wird; sind beide nicht verfügbar, wird der
     Schritt rot ("No speech API available").
- **Aufnahme & Wiedergabe** (de: "Aufnahme & Wiedergabe", en: "Record & Playback"):
  Aufnahme-Knopf startet eine Testaufnahme mit dem gewählten Mikrofon — maximal 5
  Sekunden, dann automatischer Stopp (oder manuell **Aufnahme stoppen**). Während der
  Aufnahme zeigt eine Live-Pegelanzeige mit 10 Balken den Eingangspegel. Danach
  erscheint ein Audio-Player zum Abhören, und die Aufnahme wird automatisch
  transkribiert — das Ergebnis steht unter **Transkription**. Beim ersten Mal wird ggf.
  das Sprachmodell geladen (Fortschrittsanzeige "Sprachmodell wird geladen...").

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Datenbasis**: Benutzername und E-Mail-Adresse kommen aus dem Login-Token (lokal im
  Browser gespeichert) — beim Öffnen der Seite wird KEINE Datenbank-Abfrage gemacht. Der
  Benutzername ist hier nur lesbar; Benutzerkonten (anlegen, Rollen, andere Benutzer)
  verwaltet der Administrator in den **Einstellungen**.
- **Passwort ändern**: Speichern schickt die Änderung an das Backend
  (`Accounts/ChangePasswordUser`, für jeden angemeldeten Benutzer erlaubt). Das Backend
  prüft, ob der Benutzer existiert und ob das aktuelle Passwort stimmt — bei falschem
  aktuellem Passwort schlägt die Änderung fehl. Bei Erfolg wird eine
  **Bestätigungs-E-Mail** an die Mailadresse des Benutzers gesendet und eine
  Erfolgsmeldung als Toast angezeigt; die Passwortfelder werden geleert. Es gibt KEINE
  Zwei-Faktor-Authentifizierung auf dieser Seite. Wer sein aktuelles Passwort nicht mehr
  kennt, nutzt stattdessen "Passwort vergessen?" auf der Login-Seite (Reset per E-Mail) —
  diese Karte braucht zwingend das aktuelle Passwort.
- **Profilbild**: Upload und Löschen wirken sofort (Datei-Endpoint, Dateiname
  `{Benutzer-ID}profile.{Endung}`). Das Bild erscheint danach auch als **unterstes
  Sidebar-Icon** der ganzen App — es ersetzt das generische Benutzer-Symbol des
  `open-profile`-Knopfs und wird bei jedem App-Start neu geladen. Löschen stellt das
  generische Symbol wieder her.
- **Farbschema**: wird im Browser gespeichert (localStorage) und als
  `data-theme`-Attribut auf das Dokument gesetzt — gilt sofort für die ganze App, ist
  aber **geräte-/browserspezifisch**, nicht am Benutzerkonto gespeichert.
- **Sprache**: wird ebenfalls im Browser gespeichert und sofort angewandt (alle Labels
  plus Zahlen-/Datumsformat). Die Auswahl-Liste kommt aus der Sprachkonfiguration des
  Backends: vier Kernsprachen plus die in den **Einstellungen** installierten
  Sprachpakete — installiert der Admin ein neues Sprachpaket, erscheint die Sprache hier
  in der Auswahl.
- **Mikrofon-Auswahl**: wird im Browser gespeichert und vom Sprach-Eingang des
  **Klacksy-Assistenten** mitverwendet — das hier gewählte Mikrofon ist also auch das
  Mikrofon für die Klacksy-Voice-Eingabe (Whisper-Aufnahme). Die browserintegrierte Web-
  Speech-Erkennung nutzt dagegen immer das Standard-Systemmikrofon. Die Diagnose hilft,
  Voice-Probleme einzugrenzen (fehlendes HTTPS, verweigerte Berechtigung, fehlende
  Speech-API).
- **Nicht relevant auf dieser Seite**: globale Gruppen-Auswahl, Suche, Periodenabschluss
  und Analyse-Szenarien — die Seite zeigt keine Plandaten, nur persönliche
  Einstellungen.
- **Berechtigungen**: erreichbar für alle angemeldeten Benutzer (Login genügt), kein
  Admin-Recht nötig — im Gegensatz zur Einstellungen-Seite.

### Typische Aufgaben

- Zur Seite springen — Skill `navigate_to` (Ziel "profile")
- Welche Sprachen stehen in der Sprachauswahl zur Verfügung? — Skill `list_languages`
- Neue Sprache für die Auswahl installieren (Admin, Einstellungen) — Skill
  `install_language_pack` (entfernen: `uninstall_language_pack`)
- Passwort, Profilbild, Farbschema und Mikrofon-Test bedient der Benutzer direkt auf der
  Seite — dafür gibt es keine eigenen Klacksy-Skills.

### Verwandte Seiten

- **Einstellungen** (`/workplace/settings`, nur Admin) — Benutzerverwaltung (Konten und
  Rollen anderer Benutzer) und Sprachpakete, die die Sprachauswahl dieser Seite speisen.
- **Login-Seite** — "Passwort vergessen?" für den Passwort-Reset per E-Mail, wenn das
  aktuelle Passwort unbekannt ist.
- **Klacksy-Assistent** (Chat-Panel) — nutzt das hier gewählte Mikrofon für die
  Sprach-Eingabe; der Mikrofon-Test prüft genau diese Voraussetzungen.

### Trigger-Phrasen

- "Wie ändere ich mein Passwort?"
- "Wie lade ich ein Profilbild hoch?"
- "Warum ist das Feld 'Neues Passwort wiederholen' gesperrt?"
- "Wie stelle ich die Sprache oder den Dark Mode um?"
- "How do I test my microphone for voice input?"
