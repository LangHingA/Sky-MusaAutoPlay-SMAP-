<div align="center">

<img src="src/SMAP.WPF/Assets/logo.png" width="140" alt="SMAP Logo" />

# SMAP · Sky-Musa Auto Play

An auto-play (auto-piano) helper for **Sky: Children of the Light** — reimagined as a music player, in C# WPF.

[简体中文](README.md) · [Download Releases](https://github.com/langhinga/Sky-MusaAutoPlay-SMAP-/releases) · [Old JavaFX build](legacy/JavaFX/)

</div>

---

## Overview

SMAP plays Sky's instruments for you: import or create a sheet, and it performs it in-game.
The latest release is a **full C# WPF rewrite** with a **music-player UI** — an account + collections sidebar, a cover-art library in the middle, the virtual keys on the right, and a full-width player bar at the bottom, plus a play queue, an inline cloud library, and a settings view.

Version history:

> 🎼 **v3.0.0 "Create and Share"**: introduces the native `.smap` multi-track project format, Tap/Hold notes, tempo and meter changes, and image-score import; unifies online sharing, offline, TXT/JSON and MIDI import; improves Muse sharing, announcements and background differential updates.

> 🍬 **v2.2 "YunYun"**: Practice now includes a **sheet wall, beat mode, and Game Practice** with an always-on-top window and global key listening while Sky has focus; global playback hotkeys have been redesigned; adds sheet-cover upload/display, website account avatars, and the new "Sunset Candy" theme; dropdowns and favorites gain nonlinear motion; updates can now download, install, and restart entirely in-app.

> 🎹 v2.1: introduced full-screen follow-along practice, demo playback, and playback/settings persistence.

> 🎵 v2.0: game instrument timbres filled out to **37** (incl. 27 original Sky sounds), per-instrument transpose, hold-to-sustain, and random-speed playback.

> ⚠️ SMAP must **run as administrator** (required to simulate global key presses; it auto-requests UAC on launch).

## Screenshots

These are the original v2.2 screenshots. Import, sharing and settings have changed in v3.0.

| Dark · Local library (Chinese) | Light · Cloud library (English) |
|:---:|:---:|
| ![Dark](docs/screenshots/dark_zh.png) | ![Light](docs/screenshots/light_en.png) |
| **Traditional Chinese · Settings** | **Japanese · Play queue** |
| ![TW](docs/screenshots/light_tw.png) | ![JA](docs/screenshots/dark_ja.png) |

## Features

### Playback
- **Auto-play** — simulates global key presses along the sheet timeline (uses scan codes + a short hold, so per-frame game polling doesn't miss notes).
- **Perform / Preview toggle** — the bottom-right switch: **preview by default** (sound inside the app without performing), lit = **perform** (sends game keys).
- **Player bar** — cover / title·artist·transcriber / favorite star / shuffle·prev·play·next·queue / perform mode / cave reverb / instrument / pitch (transpose) / speed (incl. random).
- **Progress bar** — a full-width thin line pinned to the container's top edge; on hover it thickens and shows a white handle + a time pill that track the play position in real time. Drag to seek, even while paused.
- **Play queue** — a slide-out queue on the right; double-click a library song to enqueue and play; three modes (repeat-all / repeat-one / shuffle); auto-advance to the next track after a **2-second gap**.
- **Remembers last state** — on exit it keeps the last played song + position; reopen to see it and resume from there; instrument / cave / perform mode / speed are remembered too.
- **Live speed control** + **global hotkeys**: F1 restart/stop · F2 pause/resume · F3/F4 speed −/+0.1x · F5/F6 previous/next track · `←`/`→` seek −/+2 seconds.

### Practice (follow-along)
- **Full-screen keyboard** — click "Practice" on the main screen; the keys zoom out of their original spot into a full-screen keyboard; `Esc` returns.
- **Follow-along hints** — highlights the next key (accent) + the next-next key (dimmed); you only advance by pressing the current key, and **chords must be held together**.
- **Demo playback** — press play to demo the whole song: sound via speaker + highlights leading the sound by one step; it **does not send game keys** in practice.
- **Sheet wall & beat mode** — a 4×8 miniature sheet follows the current step, flips pages automatically, and can advance at a configurable beat rate.
- **Game Practice** — keeps SMAP on top and listens globally for the 15 instrument keys, so follow-along continues while Sky has focus without consuming the game's input.
- **Practice hotkeys** — F1 restarts at the first cell, F2 pauses/resumes, F3/F4 change speed, F5/F6 switch songs, and `←`/`→` jump one playable cell.

### Library & Collections
- **Local library** — rich rows with cover art (title / artist / transcriber / duration); hover reveals add-to-queue / favorite / more; right-click menu with 7 actions (add to queue / add to collection / remove from library / open file location / edit / song info / upload).
- **My Collections (playlists)** — create / rename / delete / **drag to reorder** (with a drop indicator); the ⭐ star means "saved in any collection" (music-player style).
- **Inline cloud library** — browse the MuseTreehouse online library right in the middle pane, with **infinite scroll**, sort (newest / hottest / downloads) + difficulty filter, one-click download, and sheet-cover upload/display.
- **Unified import** — four tabs for online SMAP codes, offline, TXT/JSON and MIDI; native `.smap` projects and legacy sheets are supported.
- **Online sharing** — publish selected tracks with cover, artist/composer, transcriber, difficulty, tags and description; fixed version codes support downloads, project recovery, history and provenance. Publishers and authorized managers can edit song information.
- **Legacy compatibility** — TXT/JSON sheets convert automatically for sharing; metadata-only edits preserve notes, 4/8/15-key layouts, time units and extension fields.

### Authoring & Sound
- **Piano-roll editor** — multi-track `.smap` projects, Tap/Hold, tempo and meter changes, keyboard entry, triplet grid and undo/redo.
- **Image scores** — professional SVG/PNG export with project recovery for continued editing.
- **Cave reverb** — recreates Sky's cave acoustics.
- **Instruments** — **37 of them**, including 27 original Sky timbres extracted from the game (Violin / Cello / Saxophone / Ocarina / Harmonica / Pipa / Glockenspiel / Handpan / Horn / Piccolo… etc.), names localized per language.
- **Per-instrument transpose** — independent pitch offset per instrument (12 semitones + octaves), saved locally; low instruments default to their in-game register, one-click reset.
- **Hold to sustain** — wind / bowed-string instruments sound continuously while a key is held.

### UI & Settings
- **G2 continuous rounded window and controls** + **live Dark / Light / Sunset Candy themes** + **Sky-style diamond keys**.
- **Website account profile** — shows the MuseTreehouse avatar directly in the sidebar card and profile window.
- **Polished motion** — interruptible nonlinear animations for filter/sort popups, favorite stars, and practice transitions.
- **Languages** — Simplified Chinese / Traditional Chinese / English / Japanese.
- **Settings view** — language / theme / check updates / upload logs / UI scale / font scale / start countdown / key mapping / announcements and more.
- **Announcements** — view Muse-managed announcements, details and unread status in Settings.
- **Auto-requests admin** on launch + **automatic update check**; choose stable or test releases. A small pill announces background differential downloading, followed by a restart prompt when ready, without interrupting current use.

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

1. Download the latest **SMAP-Setup.exe** from [Releases](https://github.com/langhinga/Sky-MusaAutoPlay-SMAP-/releases).
2. Run it (it requests admin rights), pick an install path in the wizard; desktop / Start-menu shortcuts are created automatically.
3. Take out an instrument in-game → double-click a song in SMAP to enqueue → press the bottom ▶ or `F1` in-game.

> Downloaded and imported sheets default to `Song` next to the app; storage-location selection is not available in this version. Legacy `songs` and previous AppData libraries are copied, verified and remapped. Originals remain: verify songs and favorites before cleaning old directories.
> For a bulk sheet library, see the [`曲谱库/`](曲谱库/) folder in this repo.

Regular installations keep settings and account data in `%APPDATA%/SMAP/`. For portable use, create `portable.mode` next to the app: settings and caches go into `Data/`, while songs remain in `Song/`. Existing account data is not automatically imported into a new portable profile. Updates preserve personal songs and settings.

> In-game score mapping remains deferred to v3.1.

## Build from source

Requires Windows and [.NET SDK 10.0.403](https://dotnet.microsoft.com/download), as pinned in [global.json](global.json).

    dotnet build SMAP.slnx -c Release
    dotnet test Tests/SMAP.Tests.csproj -c Release

The main project is in `src/SMAP.WPF/`; installer and updater projects are in `Installer/` and `Updater/`. Windows x64 packages include the runtime, using single-file executables and grouped resources.

Installer packaging lives in [`Installer/`](Installer/) (`build-installer.ps1`).

## Old JavaFX build

No longer maintained; source in [`legacy/JavaFX/`](legacy/JavaFX/).

## License

See [LICENSE](LICENSE). For study/hobby use; sheet copyrights belong to their transcribers. Developed by **LANGHINGA**. See [third-party notices](docs/ThirdPartyNotices.txt).
