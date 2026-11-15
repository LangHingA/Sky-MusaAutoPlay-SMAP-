# SMAP ? Sky Music AutoPlay

[????](README.md) ? [Repository](https://github.com/langhinga/Sky-MusaAutoPlay-SMAP-)

Developed by **LANGHINGA**. A Windows music project editor, preview, practice and automatic performance app built with C#, WPF and .NET 10.

> Documentation draft for approval. This branch is the **v3.0.0-rc.3 maintenance candidate**, not a published v3.0 Stable release. Signing and final acceptance remain release gates.

## Implemented in v3.0

- Native `.smap` projects, multiple tracks, Tap / Hold, tempo and meter changes, and continued editing.
- Four import tabs: online SMAP, offline, TXT/JSON and MIDI.
- Legacy conversion; metadata-only edits retain notes, key count, time units and unknown fields rather than rebuilding a 15-key score.
- SVG/PNG professional score export and project recovery using the existing protocol.
- Muse HTTPS sharing, immutable version codes, history and provenance, with server-side ownership checks.
- Local / cloud libraries, covers, folders, playlists, preview, keyboard performance and practice.
- Three themes, Simplified Chinese / Traditional Chinese / English / Japanese, and G2 controls.
- Muse-managed announcements; settings-based update channel selection, background downloads and a restart prompt when ready.
- Signed chunk-based updates and transactional recovery. Stable requires publisher-provisioned trust; unsigned installer downloads fail closed.

**In-game score mapping remains deferred to v3.1 and is not enabled in the release candidate.**

## Installation and data

Windows x64 publish packages include the .NET runtime. The Release app requests elevation for global game input; Debug does not. Automated tests do not establish real game or human listening acceptance.

The compact application directory contains `SMAP.exe`, `SMAP.Updater.exe`, `Assets/` and `Licenses/`. Regular user storage is separate:

```text
%LOCALAPPDATA%/SMAP/{Songs,Cache,Updates,Backups}/
%APPDATA%/SMAP/  # existing settings, favorites and private sessions
```

Legacy `songs/` files are copied and SHA-256 checked. Known playlist/folder/resume references are remapped; originals remain and conflicts are not silently overwritten. Keep the old directory and backups until you verify the migration. Do not install the app into its user-data directory.

Portable mode requires an explicit `portable.mode` file next to the executable and stores data in `Data/`. An old `songs/` folder alone never selects portable mode. Existing regular-profile credentials are not automatically exported into a new portable profile.

Updates exclude Songs, Data, credentials and `portable.mode`. Recognized obsolete runtime files are archived only when exact hashes match; modified or unknown files remain. Application rollback never restores an old snapshot of personal data.

## Build and test

Use **.NET SDK 10.0.403** from [global.json](global.json) on Windows with desktop development support:

```powershell
dotnet build SMAP.slnx -c Debug
dotnet build SMAP.slnx -c Release
dotnet test Tests/SMAP.Tests.csproj -c Debug
dotnet test Tests/SMAP.Tests.csproj -c Release
./Installer/build-installer.ps1 -Dotnet 'C:/Program Files/dotnet/dotnet.exe' -OutputRoot 'C:/SMAP-build'
```

Installer outputs: `_smap_publish/`, `Payload/app.zip`, `_setup_out/SMAP-Setup.exe`. Do not commit generated output.

## Source layout

`src/SMAP.WPF/` groups Views, Controls, Core, Services, Updates, Localization, Themes and Assets. `Installer/`, `Updater/`, `Tests/` and `Tools/` have separate responsibilities. `docs/` contains documentation, `???/` preserves original scores, and `legacy/JavaFX/` archives the earlier implementation.

The reorganization does not change assembly identity, timeline semantics, cloud protocols or the `.smap` format.

## Release trust

Alpha and Stable require separate signing identities. Never put private keys in Git, MSBuild properties or packages. Stable builds accept an approved P-256 public key and KeyId; signing tools must match that identity. Full installers require signed publisher metadata plus an exact SHA-256 check, not merely HTTPS, size or an MZ header.

This candidate has no provisioned production Stable key and makes no Authenticode certificate claim. Signing, real UAC/game/listening checks and final release approval remain pending. See the [release notes draft](docs/RELEASE-NOTES-v3.0-DRAFT.md).

## License

See [LICENSE](LICENSE) and [third-party notices](docs/ThirdPartyNotices.txt). Respect score ownership and applicable platform rules.
