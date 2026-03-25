# Zsnd UI
 A Graphical User Interface for Handling Raven Software's Zsnd Format, used in their X-Men Legends and Marvel Ultimate Alliance games.

## Table of contents:

* [Credits](#credits)
* [Requirements](#requirements)
* [Features](#features)
* [Planned Features](#planned-features)
* [Usage Instructions](#usage-instructions)
* [Coding Instructions](#coding-instructions)
* [Build Instructions](#build-instructions)
* [Changelog](#changelog)
<br/><br/>

## Credits
- @nikita488, Winstrol, Norrin Radd: Zsnd and formats research
- Outsider, Teancum: Zsnd modding tutorials (events)
- [sdecoret](https://stock.adobe.com/images/sunrise-over-group-of-planets-in-space/105677222), [tadp0l3](https://www.deviantart.com/tadp0l3/art/Nocturnal-skies-146160144): Banner images
- Using the MarvelMods logo by Outsider, based on Marvel's classic comic logo.
- Using samples from the WinUI and Windows App SDK tutorials from [Microsoft](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- Thanks to BloodyMares for testing the hash generators.
<br/><br/>

## Requirements
- Windows 10+
<br/><br/>

## Features
- Open, extract, edit and save Zsnd files of various platforms (.zss, .zsm, .ens, .enm files)
- Open, edit and save Raven-Formats Zsnd data files (.json)
- Convert Zsnd sounds of various formats to standard .wav sounds
- Convert standard .wav sounds to Zsnd .wav or .xbadpcm formats, for saving Zsnd files
- Load .wav, .xma, .xbadpcm, .dsp, and .vag files, depending on the platform (update planned)
- Add new sounds directly from added (.wav) files or add files to existing sounds
- Play various formats using [vgmstream](https://github.com/vgmstream/vgmstream-releases/releases/download/nightly/vgmstream-win64.zip) (not needed for .wav and .xbadpcm - must be added manually, 64-bit only, instructions will follow)
- Manage Zsnd files side-by-side with a split-view, multiple tabs, and windows support
- Copy sound entries with drag & drop
- Manage hashes with a hash event editor
- Hash generators to reverse generate hashes from Zsnd and to generate default character hashes from added file names
<br/><br/>

## Planned Features
Note: Many of these features depend on user feedback
- Finding and fixing bugs
- Add more user settings
- Support selecting multiple files and folders when browsing for files to add
- Add support for other files than .wav files when adding directories
- Add support for batch extraction
- Add user control to save (backup) settings
- Add user control over how files are replaced, backed up, or warned (needs testing)
- Possibly add user control over extract formats for .wav, .xbadpcm and .vag (normal, raw)
- Possibly improve app response on tasks that are too fast to show regular responses (handled by success messages?, see below)
- Possibly make recently closed tabs shared among windows
- If a window is closed, add all tabs to the recently closed tabs (or add a recently closed window feature)
- Possibly add a saved recent files list
- More play features (stop, pause, etc.)
- Detailed fail messages, when sounds fail to play
- Add success messages on save, extract (and possibly load) tasks
- Possibly add events, depending on use cases
<br/><br/>

## Usage Instructions
Will follow
<br/><br/>

## Coding Instructions
- [WinUI3 projects with Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/create-your-first-winui3-app)
- The language is [C#](https://www.w3schools.com/cs/index.php) and Xaml/XML (C# [rescources](https://www.reddit.com/r/csharp/comments/2umooh/what_are_your_favorite_c_online_resources/) and [tutorials](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/) ).
<br/><br/>

## Build Instructions
- Use Visual Studio and install .Net with its installer dialogue (.Net 10). Install Windows App SDK in the same dialogue (.Net and the Windows App SDK are required to build a C# WinUI project).
- Make sure to add the dependencies before building, as always.
- I recommend to leave the project as self contained (no dependencies), since WinUI is contained anyway.
- A Windows App SDK can be built [unpackaged](https://github.com/microsoft/WindowsAppSDK-Samples/tree/f1a30c2524c785739fee842d02a1ea15c1362f8f/Samples/SelfContainedDeployment/cs-winui-unpackaged) or [packaged](https://github.com/microsoft/WindowsAppSDK-Samples/tree/f1a30c2524c785739fee842d02a1ea15c1362f8f/Samples/SelfContainedDeployment/cs-winui-packaged) (MSIX). As a WinUI3 project, it can be easily re-targeted to a UWP project, but it lacks permission (file access) and signature details.
- The project can be made cross platform through [.Net MAUI](https://dotnet.microsoft.com/en-us/apps/maui) or other platforms, like [UNO](https://platform.uno/). Let me know, if you're interested in that.
<br/><br/>

## Changelog
See [commits](commits/master/)
