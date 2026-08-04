#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Elevated runner: probe + full description-only upload, logged to upload_run.log.

Launched via UAC (RunAs) so the Steam API can connect to an elevated Steam client.
"""
import io
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
LOG = os.path.join(HERE, "upload_run.log")
APP_ID = 255710
ITEM_ID = 3773802930

sys.path.insert(0, HERE)
os.chdir(HERE)

import upload_desc  # noqa: E402


def log(msg):
    with io.open(LOG, "a", encoding="utf-8") as f:
        f.write(msg + "\n")
    print(msg, flush=True)


def main():
    log("=== run started %s ===" % time.strftime("%Y-%m-%d %H:%M:%S"))
    steam = upload_desc.STEAMWORKS()
    try:
        steam.initialize()
    except Exception as e:
        log("INIT FAILED: %s" % e)
        log("Steam client must be running and logged in on the owner account.")
        return 1

    log("INIT OK")
    log("logged_on: %s" % steam.LoggedOn())
    try:
        log("persona: %s" % steam.GetPersonaName().decode("utf-8", "replace"))
    except Exception as e:
        log("persona error: %s" % e)
    try:
        log("steamid: %s" % steam.GetSteamID())
    except Exception as e:
        log("steamid error: %s" % e)
    log("owns_app_%d: %s" % (APP_ID, steam.IsSubscribedApp(APP_ID)))

    updates = upload_desc.collect_descriptions()
    over = [k for k, u in updates.items() if u["bytes"] > upload_desc.MAX_DESC_BYTES]
    for k in over:
        updates.pop(k)
    if not updates:
        log("nothing to upload (all skipped/over-size)")
        steam.unload()
        return 1

    log("uploading %d language(s), title + description" % len(updates))
    ok = 0
    failed = []
    try:
        for lang in sorted(updates):
            u = updates[lang]
            log("uploading %s (%d bytes)" % (lang, u["bytes"]))
            try:
                handle = steam.Workshop.StartItemUpdate(APP_ID, ITEM_ID)
                if not handle:
                    log("  ! StartItemUpdate failed")
                    failed.append(lang)
                    continue
                if not steam.Workshop.SetItemDescription(handle, u["text"]):
                    log("  ! SetItemDescription failed")
                    failed.append(lang)
                    continue
                if upload_desc.submit_one(steam, handle, lang):
                    ok += 1
                else:
                    failed.append(lang)
            except Exception as e:
                log("  !! exception: %s: %s" % (type(e).__name__, e))
                failed.append(lang)
    finally:
        steam.unload()

    log("DONE ok=%d failed=%d" % (ok, len(failed)))
    if failed:
        log("failed: %s" % ", ".join(failed))
        return 1
    log("See https://steamcommunity.com/sharedfiles/filedetails/?id=%d" % ITEM_ID)
    return 0


if __name__ == "__main__":
    sys.exit(main())
