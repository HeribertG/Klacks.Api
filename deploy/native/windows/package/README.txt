Klacks for the office PC (Windows 10/11, 64-bit) — preview

1. Extract this folder completely (e.g. to the desktop or a USB stick).
2. Double-click "Installieren.cmd" and confirm the administrator prompt with "Yes".
   If Windows shows "Windows protected your PC": "More info" -> "Run anyway".
   The installation then asks for the country (public holidays, time zone, working-time rules); Enter takes the
   country of this PC, "0" sets up no region profile.
3. The installation takes a few minutes; the first start also downloads about 700 MB of AI models.
4. Then open http://localhost:5180 in the browser.
   First sign-in: admin@test.com / P@ssw0rt1 — Klacks then asks you to create your own admin account.

Report: "Klacks-Installationsbericht-<date>.txt" is written next to "Installieren.cmd" (or to the desktop when the
folder is read-only). Please send this file if something goes wrong.

Updates: Klacks updates itself (service "Klacks Updater"), signature-verified.

Uninstall (PowerShell as administrator, in this folder):
  powershell -ExecutionPolicy Bypass -File scripts\Uninstall-Klacks.ps1            (keeps the data)
  powershell -ExecutionPolicy Bypass -File scripts\Uninstall-Klacks.ps1 -RemoveData  (also deletes all data)

Diagnostics (PowerShell as administrator):
  powershell -ExecutionPolicy Bypass -File scripts\Get-KlacksDiagnostics.ps1
