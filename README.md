# EDL Chapters for Emby

Import commercial breaks from adjacent `.edl` files as chapters in Emby’s database. Video files are unchanged. No chapter backups are created.

## Install

1. Download `EdlChapters.dll` from Releases.
2. Stop Emby Server, place the DLL in its plugins folder, and restart Emby.
3. Open Plugins → EDL Chapters → Settings.
4. Set the server-local recordings folder and save. The initial folder is `/mnt/Medialibrary/Recorded TV`; change it to match your server.

Automatic imports require server-local paths in Emby’s library. Network-path translation is not included.

## Import chapters

Enable **scheduled imports** and save to allow the existing **Import commercial EDL chapters** task to import recordings. Manage its triggers under Scheduled Tasks → EDL Chapters. The default interval is 15 minutes; the checkbox does not create another task or reset its schedule.

To test a recording, enter its Emby item ID, preview the EDL, and import. The optional recording path must point to the video, not the EDL. The `.edl` must be beside the video with the same base filename. Active recordings are skipped.

Commercial chapters are named Advertisement; programme boundaries are named Programme. Ordinary chapter points inside breaks are replaced automatically. Overlapping intro or credits markers prevent that recording from being imported. EDL actions 0 and 3 are treated as commercial ranges; mute and scene actions are ignored.

These chapters do not guarantee automatic commercial skipping in every Emby client. Client or plugin support determines skipping behavior. Stop and restart playback after importing. A metadata refresh may replace chapters; scheduled imports can recreate them.

## Chapter thumbnails

The optional **Generate chapter thumbnails after importing** setting queues Emby’s library-wide thumbnail extraction task after changed imports. It respects each library’s settings. Configure the library thumbnail interval as Chapter markers. Preview and unchanged imports do not trigger extraction. This feature needs live verification.

## Build and test

Requires the .NET SDK.

```sh
dotnet build EdlChapters.csproj -c Release
dotnet run --project Tests/Tests.csproj
```

The plugin targets .NET Standard 2.0 and references Emby SDK 4.7.3. Developed for Emby Server 4.10.1.0; other versions have not been verified. Release 1.0.0.5 builds successfully; parser, chapter-planning and thumbnail-queue checks pass.

## License and attribution

GPL-3.0-or-later. This project originated from a modified [ChapterApi](https://github.com/faush01/ChapterApi) project. It is an independent plugin and does not require ChapterApi to be installed. See LICENSE.
