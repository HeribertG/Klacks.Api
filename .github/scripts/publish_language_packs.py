# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
"""Uploads language packs from Plugins/Languages to the Klacks Store with the release version.

Usage: publish_language_packs.py [--dry-run] <version> <code> [<code> ...]
The upload key comes from the environment variable MARKETPLACE_UPLOAD_KEY. The pack version in the store is the
Klacks release version (the manifest's own counter is overwritten), so the store shows which Klacks release a pack
belongs to. Existing published packs stay published; the store keeps only the latest version of each pack.
"""
import json
import os
import sys
import urllib.error
import urllib.request

STORE_API_URL = "https://klacks-software.ch/store/api/packages"
LANGUAGES_DIR = os.path.join("Plugins", "Languages")
UPLOAD_KEY_ENV = "MARKETPLACE_UPLOAD_KEY"
DRY_RUN_FLAG = "--dry-run"
API_KEY_HEADER = "X-Api-Key"
REQUEST_TIMEOUT_SECONDS = 120
DEFAULT_MIN_KLACKS_VERSION = "1.0.0"
OPTIONAL_FILES = {
    "docsJson": "docs.json",
    "countriesJson": "countries.json",
    "statesJson": "states.json",
    "calendarRulesJson": "calendar-rules.json",
}
BUNDLE_KEY_FILES = {"manifest.json", "translations.json", *OPTIONAL_FILES.values()}
JSON_SUFFIX = ".json"


def read_text(path):
    with open(path, encoding="utf-8-sig") as handle:
        return handle.read()


def build_payload(code, version):
    pack_dir = os.path.join(LANGUAGES_DIR, code)
    manifest = json.loads(read_text(os.path.join(pack_dir, "manifest.json")))
    manifest["version"] = version
    translations_json = read_text(os.path.join(pack_dir, "translations.json"))
    payload = {
        "code": code,
        "name": manifest["name"],
        "displayName": manifest["displayName"],
        "speechLocale": manifest.get("speechLocale", ""),
        "version": version,
        "coverage": manifest.get("coverage", 0),
        "description": manifest.get("description", ""),
        "minKlacksVersion": manifest.get("minKlacksVersion", DEFAULT_MIN_KLACKS_VERSION),
        "translationCount": len(json.loads(translations_json)),
        "manifestJson": json.dumps(manifest, ensure_ascii=False, indent=2),
        "translationsJson": translations_json,
    }
    for field, file_name in OPTIONAL_FILES.items():
        path = os.path.join(pack_dir, file_name)
        if os.path.exists(path):
            payload[field] = read_text(path)
    extra_files = {
        file_name: json.loads(read_text(os.path.join(pack_dir, file_name)))
        for file_name in sorted(os.listdir(pack_dir))
        if file_name.endswith(JSON_SUFFIX) and file_name not in BUNDLE_KEY_FILES
    }
    if extra_files:
        payload["extraFilesJson"] = json.dumps(extra_files, ensure_ascii=False)
    return payload


def upload(payload, key):
    request = urllib.request.Request(
        STORE_API_URL,
        data=json.dumps(payload, ensure_ascii=False).encode("utf-8"),
        headers={"Content-Type": "application/json", API_KEY_HEADER: key},
        method="POST",
    )
    with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT_SECONDS) as response:
        return response.status


def published_version(code):
    with urllib.request.urlopen(f"{STORE_API_URL}/{code}", timeout=REQUEST_TIMEOUT_SECONDS) as response:
        return json.loads(response.read()).get("version")


def main():
    args = sys.argv[1:]
    dry_run = DRY_RUN_FLAG in args
    args = [arg for arg in args if arg != DRY_RUN_FLAG]
    if len(args) < 2:
        print("usage: publish_language_packs.py [--dry-run] <version> <code> [<code> ...]", file=sys.stderr)
        return 2
    version, codes = args[0], args[1:]
    key = os.environ.get(UPLOAD_KEY_ENV, "")
    if not key and not dry_run:
        print(f"{UPLOAD_KEY_ENV} is not set", file=sys.stderr)
        return 2

    failed = []
    for code in codes:
        try:
            payload = build_payload(code, version)
            if dry_run:
                extra = sorted(json.loads(payload.get("extraFilesJson", "{}")))
                print(f"{code}: would upload version {version}, {payload['translationCount']} translations, "
                      f"{len(extra)} further files, store has {published_version(code)}")
                continue
            status = upload(payload, key)
            live = published_version(code)
            if live != version:
                raise RuntimeError(f"store reports version {live}")
            print(f"{code}: HTTP {status}, store version {live}")
        except (OSError, ValueError, KeyError, RuntimeError, urllib.error.URLError) as error:
            print(f"::error::{code}: {error}")
            failed.append(code)

    if failed:
        print(f"Failed: {', '.join(failed)}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
