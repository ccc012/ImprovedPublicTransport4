#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Update the localized Workshop titles and descriptions of Improved Public Transport 4.

Reads  ../workshop-description-<steam_lang>.txt  (the Projeto-Steam folder) and
pushes each one through the Steamworks SteamUGC API using SetItemUpdateLanguage.
Each language gets BOTH its title and description re-set in the same update:
Steam clears the localized title when only the description is submitted, so the
title is always written together with the description. Content, preview image
and tags are NOT modified.

Requirements
------------
  * Steam client running and logged in on the account that OWNS the item.
  * That account must own Cities: Skylines (app 255710) so the Steam API boots
    for this app id.
  * The owner account must have accepted the Steam Workshop Legal Agreement.
  * 64-bit Python (the binding loads SteamworksPy64.dll + steam_api64.dll that
    live in this same folder).

Usage
-----
  python upload_desc.py              # push every language (asks to confirm)
  python upload_desc.py --dry-run    # show plan, submit nothing
  python upload_desc.py --lang german,brazilian --yes
  python upload_desc.py --item 3773802930 --appid 255710

Steam language codes are the file suffixes (english, schinese, tchinese,
koreana, brazilian, latam, ...). The "en" file is treated as an alias of
"english".
"""

import argparse
import os
import re
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
PROJETO_STEAM = os.path.normpath(os.path.join(HERE, ".."))
DESC_RE = re.compile(r"^workshop-description-(.+)\.txt$")
LANG_ALIASES = {"en": "english"}
TITLE = "Improved Public Transport 4 (IPT4)"
MAX_DESC_BYTES = 8000
SUBMIT_TIMEOUT_SECONDS = 300
POLL_INTERVAL_SECONDS = 0.5

sys.path.insert(0, HERE)
from steamworks import STEAMWORKS  # noqa: E402
from steamworks.enums import EItemUpdateStatus, EResult  # noqa: E402


def collect_descriptions():
    """Return {steam_lang: {"source", "bytes", "text"}} for every description file."""
    updates = {}
    for name in sorted(os.listdir(PROJETO_STEAM)):
        m = DESC_RE.match(name)
        if not m:
            continue
        lang = LANG_ALIASES.get(m.group(1), m.group(1))
        path = os.path.join(PROJETO_STEAM, name)
        with open(path, "r", encoding="utf-8-sig") as f:
            text = f.read()
        updates[lang] = {
            "source": name,
            "bytes": len(text.encode("utf-8")),
            "text": text,
        }
    return updates


def submit_one(steam, handle, lang):
    """Set the update language and title, then submit and block until Steam answers."""

    if not steam.Workshop.SetItemUpdateLanguage(handle, lang.encode()):
        print("  ! SetItemUpdateLanguage failed for '%s'" % lang)
        return False

    if not steam.Workshop.SetItemTitle(handle, TITLE):
        print("  ! SetItemTitle failed for '%s'" % lang)
        return False

    holder = {"done": False, "result": None}

    def on_updated(result):
        holder["done"] = True
        holder["result"] = result

    steam.Workshop.SubmitItemUpdate(handle, None, callback=on_updated, override_callback=True)

    last_status = None
    start = time.time()
    while not holder["done"]:
        steam.run_callbacks()
        if time.time() - start > SUBMIT_TIMEOUT_SECONDS:
            print("  ! Timed out waiting for Steam to process the submit.")
            return False
        progress = steam.Workshop.GetItemUpdateProgress(handle)
        status = progress["status"]
        if status != EItemUpdateStatus.INVALID and status != last_status:
            print("    status: %s" % status.name.replace("_", " ").lower())
            last_status = status
        time.sleep(POLL_INTERVAL_SECONDS)

    res = holder["result"]
    if res is None:
        print("  ! No result returned by Steam.")
        return False

    code = EResult(res.result)
    if code != EResult.OK:
        print("  ! Submit failed: %s" % code.name)
        return False

    if res.userNeedsToAcceptWorkshopLegalAgreement:
        print("  ! The Steam account must accept the Workshop Legal Agreement first.")
        return False

    return True


def main():
    parser = argparse.ArgumentParser(description="Update only IPT4's localized Workshop descriptions.")
    parser.add_argument("--dry-run", action="store_true", help="show what would be uploaded, submit nothing")
    parser.add_argument("--yes", "-y", action="store_true", help="skip the confirmation prompt")
    parser.add_argument("--lang", default=None, help="comma separated Steam languages, e.g. german,brazilian")
    parser.add_argument("--item", type=int, default=3773802930, help="published file id (default: IPT4)")
    parser.add_argument("--appid", type=int, default=255710, help="Steam app id (default: Cities: Skylines)")
    args = parser.parse_args()

    updates = collect_descriptions()
    if args.lang:
        wanted = {x.strip() for x in args.lang.split(",") if x.strip()}
        updates = {k: v for k, v in updates.items() if k in wanted}

    if not updates:
        print("No description files found in %s" % PROJETO_STEAM)
        return 1

    print("Plan for item %d (app %d) - description only:" % (args.item, args.appid))
    over = []
    for lang in sorted(updates):
        u = updates[lang]
        mark = ""
        if u["bytes"] > MAX_DESC_BYTES:
            mark = "   !! over %d bytes" % MAX_DESC_BYTES
            over.append(lang)
        print("  %-12s %6d bytes   <== %s%s" % (lang, u["bytes"], u["source"], mark))
    print("  %d language(s)." % len(updates))

    if args.dry_run:
        print("Dry run: nothing was submitted.")
        return 0

    if over:
        print("Skipping over-size languages (Steam rejects > %d bytes)." % MAX_DESC_BYTES)
        for lang in over:
            updates.pop(lang)

    if not updates:
        print("Nothing left to upload.")
        return 1

    if not args.yes:
        try:
            answer = input("Submit these descriptions now? [y/N] ").strip().lower()
        except (EOFError, KeyboardInterrupt):
            answer = "n"
        if answer != "y":
            print("Cancelled.")
            return 0

    os.chdir(HERE)
    steam = STEAMWORKS()
    try:
        steam.initialize()
        print("Steam API initialized. Owner: %s" % steam.GetPersonaName().decode("utf-8", "replace"))
    except Exception as e:
        print("Cannot initialize Steam: %s" % e)
        print("Make sure the Steam client is running and logged in.")
        return 1

    ok = 0
    failed = []
    try:
        for lang in sorted(updates):
            u = updates[lang]
            print("Uploading %s (%d bytes)..." % (lang, u["bytes"]))
            handle = steam.Workshop.StartItemUpdate(args.appid, args.item)
            if not handle:
                print("  ! StartItemUpdate failed for '%s'." % lang)
                failed.append(lang)
                continue
            if not steam.Workshop.SetItemDescription(handle, u["text"]):
                print("  ! SetItemDescription failed for '%s'." % lang)
                failed.append(lang)
                continue
            if submit_one(steam, handle, lang):
                ok += 1
            else:
                failed.append(lang)
    finally:
        steam.unload()

    print("")
    print("Done. %d submitted, %d failed." % (ok, len(failed)))
    if failed:
        print("Failed: %s" % ", ".join(failed))
        return 1
    print("See https://steamcommunity.com/sharedfiles/filedetails/?id=%d" % args.item)
    return 0


if __name__ == "__main__":
    sys.exit(main())
