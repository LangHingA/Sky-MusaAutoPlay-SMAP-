<div align="center">

<img src="src/SMAP.WPF/Assets/logo.png" width="140" alt="SMAP Logo" />

# SMAP · Sky-Musa Auto Play

An auto-play (auto-piano) helper for **Sky: Children of the Light** — reimagined as a music player, in C# WPF.

[简体中文](README.md) · [Download Releases](https://github.com/langhinga/Sky-MusaAutoPlay-SMAP-/releases) · [Old JavaFX build](legacy/JavaFX/)

</div>

---

## Overview

Developed by **LANGHINGA**. This delivery is **v3.0.0 Stable**. Release artifacts are prepared with the final version; this README and the release notes await user review before public release.

SMAP plays Sky's instruments for you: import or create a sheet, and it performs it in-game.
The latest release is a **full C# WPF rewrite** with a **music-player UI** — an account + collections sidebar, a cover-art library in the middle, the virtual keys on the right, and a full-width player bar at the bottom, plus a play queue, an inline cloud library, and a settings view.

> 🎼 **v3.0.0**: native `.smap` multi-track projects, Tap/Hold, tempo and meter changes, four import tabs, Muse versioned sharing, image-score recovery, announcements and signed background differential updates. Legacy migration keeps original files; songs are grouped in `Song/` beside the program.

> 🍬 **v2.2 "YunYun"**: Practice now includes a **sheet wall, beat mode, and Game Practice** with an always-on-top window and global key listening while Sky has focus; global playback hotkeys have been redesigned; adds sheet-cover upload/display, website account avatars, and the new "Sunset Candy" theme; dropdowns and favorites gain nonlinear motion; updates can now download, install, and restart entirely in-app.

> 🎹 v2.1: introduced full-screen follow-along practice, demo playback, and playback/settings persistence.

> 🎵 v2.0: game instrument timbres filled out to **37** (incl. 27 original Sky sounds), per-instrument transpose, hold-to-sustain, and random-speed playback.

> ⚠️ SMAP must **run as administrator** (required to simulate global key presses; it auto-requests UAC on launch).

## Screenshots

The original screenshots are retained to show the visual style. Import, sharing and update screens have changed in v3.0.

| Dark · Local library (Chinese) | Light · Cloud library (English) |
|:---:|:---:|
| ![Dark](docs/screenshots/dark_zh.png) | ![Light](docs/screenshots/light_en.png) |
| **Traditional Chinese · Settings** | **Japanese · Play queue** |
| ![TW](docs/screenshots/light_tw.png) | ![JA](docs/screenshots/dark_ja.png) |

## Features

### Playback
- **Auto-play** — simulates global key presses along the sheet timeline (uses scan codes + a short hold, so per-frame game polling doesn't miss notes).
- **Perform / Preview toggle** — the bottom-right switch: **preview by default** (speaker, won't touch the game), lit = **perform** (sends game keys).
- **Player bar** — cover / title·artist·transcriber / favorite star / shuffle·prev·play·next·queue / perform mode / cave reverb / instrument / pitch (transpose) / speed (incl. random).
- **Progress bar** — a full-width thin line pinned to the container's top edge; on hover it thickens and shows a white handle + a time pill that track the play position in real time. Drag to seek, even while paused.
- **Play queue** — a slide-out queue on the right; double-click a library song to enqueue and play; three modes (repeat-all / repeat-one / shuffle); auto-advance to the next track after a **2-second gap**.
- **Remembers last state** — on exit it keeps the last played song + position; reopen to see it and resume from there; instrument / cave / perform mode / speed are remembered too.
- **Live speed control** + **global hotkeys**: F1 restart/stop · F2 pause/resume · F3/F4 speed −/+0.1x · F5/F6 previous/next track · `←`/`→` seek −/+2 seconds.

### Practice (follow-along)
- **Full-screen keyboard** — click "Practice" on the main screen; the keys zoom out of their original spot into a full-screen keyboard (springy, interruptible animation); `Esc` returns.
- **Follow-along hints** — highlights the next key (accent) + the next-next key (dimmed); you only advance by pressing the current key, and **chords must be held together**.
- **Demo playback** — press play to demo the whole song: sound via speaker + highlights leading the sound by one step; it **never sends game keys** in practice.
- **Sheet wall & beat mode** — a 4×8 miniature sheet follows the current step, flips pages automatically, and can advance at a configurable beat rate.
- **Game Practice** — keeps SMAP on top and listens globally for the 15 instrument keys, so follow-along continues while Sky has focus without consuming the game's input.
- **Practice hotkeys** — F1 restarts at the first cell, F2 pauses/resumes, F3/F4 change speed, F5/F6 switch songs, and `←`/`→` jump one playable cell.

### Library & Collections
- **Local library** — rich rows with cover art (title / artist / transcriber / duration); hover reveals add-to-queue / favorite / more; right-click menu with 7 actions (add to queue / add to collection / remove from library / open file location / edit / song info / upload).
- **My Collections (playlists)** — create / rename / delete / **drag to reorder** (with a drop indicator); the ⭐ star means "saved in any collection" (music-player style).
- **Inline cloud library** — browse the MuseTreehouse online library right in the middle pane, with **infinite scroll**, sort (newest / hottest / downloads) + difficulty filter, one-click download, and sheet-cover upload/display.
- **Unified import** — four tabs for online SMAP sharing, offline, TXT/JSON and MIDI; native `.smap` projects and legacy sheets are supported.
- **Online sharing** — Muse HTTPS publishing and downloads, immutable version codes, history and provenance. Publishers and authorized managers can edit song information; the server authorizes management operations.
- **Legacy protection** — metadata-only edits preserve notes, key counts, time units and valid extension fields without rewriting other songs in a multi-song file.

### Authoring & Sound
- **Piano-roll editor** — native `.smap` multi-track projects, Tap/Hold notes, tempo and meter changes, keyboard entry, triplet grid, undo/redo and continued editing.
- **Professional image scores** — SVG/PNG export and project recovery using the existing recovery protocol.
- **Version boundary** — in-game score mapping remains deferred to v3.1 and is not enabled.
- **Cave reverb** — recreates Sky's cave acoustics.
- **Instruments** — **37 of them**, including 27 original Sky timbres extracted from the game (Violin / Cello / Saxophone / Ocarina / Harmonica / Pipa / Glockenspiel / Handpan / Horn / Piccolo… etc.), names localized per language.
- **Per-instrument transpose** — independent pitch offset per instrument (12 semitones + octaves), saved locally; low instruments default to their in-game register, one-click reset.
- **Hold to sustain** — wind / bowed-string instruments sound continuously while a key is held.

### UI & Settings
- **G2 continuous rounded controls and custom window** + **live Dark / Light / Sunset Candy themes** + **Sky-style diamond keys** (with a flip animation on trigger).
- **Website account profile** — shows the MuseTreehouse avatar directly in the sidebar card and profile window.
- **Polished motion** — interruptible nonlinear animations for filter/sort popups, favorite stars, and practice transitions.
- **Languages** — Simplified Chinese / Traditional Chinese / English / Japanese.
- **Settings view** (slides in from the middle/right panels) — language / theme / check updates / upload logs / UI scale / font scale / start countdown / key mapping.
- **Announcements** — view Muse-managed announcements, details and unread state in Settings.
- **Auto-requests admin** on launch + **automatic update check**. Select the stable or test channel in Settings; a small pill announces background downloading, and a separate prompt offers restart when ready. The running program is not replaced immediately.

## Shortcuts

**Global hotkeys** (work in-game too):

| Key | Action | Key | Action |
|---|---|---|---|
| `F1` | Restart / Stop | `F2` | Pause / Resume |
| `F3` | Speed −0.1x | `F4` | Speed +0.1x |
| `F5` | Previous track | `F6` | Next track |
| `←` | Back 2 seconds | `→` | Forward 2 seconds |

**Virtual keys** (15 keys, remappable in Settings):

| Register | Keys |
|---|---|
| High | `Y` `U` `I` `O` `P` |
| Mid | `H` `J` `K` `L` `;` |
| Low | `N` `M` `,` `.` `/` |

> Wind / bowed-string instruments sustain while you hold a key.

## Install / Use

1. Download **SMAP-Setup.exe** for the desired version from [Releases](https://github.com/langhinga/Sky-MusaAutoPlay-SMAP-/releases). v3.0.0 uses the stable update channel; the test channel is reserved for subsequent prereleases.
2. Run it (it requests admin rights), pick an install path in the wizard; desktop / Start-menu shortcuts are created automatically.
3. Take out an instrument in-game → double-click a song in SMAP to enqueue → press the bottom ▶ or `F1` in-game.

> Downloaded and imported songs default to `Song/` beside the program. This version does not offer a storage-location setting. Existing settings, favorites and private sessions remain in `%APPDATA%/SMAP/`.
> For a bulk sheet library, see the [`曲谱库/`](曲谱库/) folder in this repo.

Migration from legacy `songs/` and the previous `%LOCALAPPDATA%/SMAP/Songs/` location copies files, verifies SHA-256 and remaps known references. Originals remain, and name conflicts are not silently overwritten. Verify songs, favorites and sharing associations before cleaning old directories. Do not install into the personal-data directory itself.

Portable mode requires an explicit `portable.mode` file next to the program and stores settings, caches and update staging in `Data/`; songs remain in `Song/` beside the program. An old `songs/` directory alone never enables it; regular-profile credentials are not automatically exported.

The compact program directory primarily contains `SMAP.exe`, `SMAP.Updater.exe`, `Assets/`, `Licenses/`, the user library `Song/` and a signed installed manifest. Caches, update staging and backups live under `%LOCALAPPDATA%/SMAP/`. Updates preserve personal songs and settings; program rollback does not roll back user data.

## Build from source

Requires Windows and [.NET SDK 10.0.403](https://dotnet.microsoft.com/download), as pinned in [global.json](global.json).

```powershell
dotnet build SMAP.slnx -c Debug
dotnet build SMAP.slnx -c Release
dotnet test Tests/SMAP.Tests.csproj -c Debug
dotnet test Tests/SMAP.Tests.csproj -c Release
./Installer/build-installer.ps1 -Dotnet 'C:/Program Files/dotnet/dotnet.exe' -OutputRoot 'C:/SMAP-build'
```

The main project is in `src/SMAP.WPF/`, grouped into Views, Controls, Core, Services and Updates. `Installer/`, `Updater/`, `Tests/` and `Tools/` have separate responsibilities. Packaging produces `_smap_publish/`, `Payload/app.zip` and `_setup_out/SMAP-Setup.exe`; Windows x64 installers include the runtime.

Build and isolated-test temporary storage must stay within **5 GB (5,000,000,000 bytes) combined**. Set `SMAP_BUILD_SCRATCH_ROOT` to one F-drive development scratch directory and run `Tools/CheckBuildStorage.ps1` with a reservation for the next operation. Installer packaging checks this budget. Never delete user data or recovery backups to make room.

## Update and release trust

The source branches are main for stable and beta for testing. The client discovers GitHub Releases: stable releases for the stable channel, prereleases for testing. Alpha and Stable use independent P-256 signing identities. Never include private keys in Git or release assets. Updates require valid manifest signatures and content SHA-256 checks; automatic full-installer updates also require trusted signed release metadata.

RC.4 to RC.5 passed real GitHub HTTPS differential download, verification, reconstruction and user-confirmed restart acceptance. This does not establish an online Stable-to-Stable test. Windows Authenticode is not configured; signed update metadata is distinct from a Windows publisher certificate. Real game performance and human listening require separate acceptance.

The v3.0.0 release documentation awaits user review. See the [release notes draft](docs/RELEASE-NOTES-v3.0-DRAFT.md).

## Old JavaFX build

No longer maintained; source in [`legacy/JavaFX/`](legacy/JavaFX/).

## License

See [LICENSE](LICENSE) and [third-party notices](docs/ThirdPartyNotices.txt). For study/hobby use; sheet copyrights belong to their transcribers.
