@echo off
rem Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
rem Double-click starter of the Klacks office-PC installation; everything else happens in scripts\Install-KlacksPackage.ps1.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Install-KlacksPackage.ps1" %*
