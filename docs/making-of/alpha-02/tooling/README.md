❰ [Making of alpha.02](../README.md) ❬ Tooling

# Tooling

| CONTENTS |
|:--|
| [Toolchain](#toolchain) |
| [Shared build settings](#shared-build-settings) |
| [Building, running and publishing](#building-running-and-publishing) |
| [Embedded assets](#embedded-assets) |
| [macOS quit handling](#macos-quit-handling) |
| [High-DPI windows](#high-dpi-windows) |
| [The rename](#the-rename) |
| [Repository housekeeping](#repository-housekeeping) |

## Toolchain

| Tool | Version | Where it is set |
|:--|:--|:--|
| .NET SDK | 11.0.100 (rolls forward to later 11.0 feature bands) | `src/global.json` |
| Target framework | `net11.0` | `src/Directory.Build.props` |
| C# | 15.0 | `src/Directory.Build.props` |
| MonoGame | DesktopGL 3.8.6.1 (SDL2 and OpenGL) | `EchoesOfTheShardveil.Desktop.csproj` |
| Solution format | `.slnx` | `src/EchoesOfTheShardveil.slnx` |
| Editor workspace | VS Code | `.github/workspace/EchoesOfTheShardveil.code-workspace` |

Development happens on an Apple Silicon Mac, so the default publish target is `osx-arm64`.

## Shared build settings

Early on, each project file repeated the same settings. A clean-up moved them into `src/Directory.Build.props`, which applies to every project:

```xml
<TargetFramework>net11.0</TargetFramework>
<LangVersion>15.0</LangVersion>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<AnalysisLevel>latest-recommended</AnalysisLevel>
```

`AnalysisLevel` turns on the recommended .NET code analyzers. With the shared file in place, the Core project file is just `DebugType` set to `embedded`, and the Desktop project file only holds what is specific to the app: the Core reference, the MonoGame package, the embedded assets, and publish settings.

`global.json` pins the SDK so every machine builds with the same compiler and analyzers.

## Building, running and publishing

From `src/` (these commands are kept in the scratch pad under `.github/development/scratch/`):

```bash
# Build and run
dotnet build EchoesOfTheShardveil.slnx
dotnet run --project EchoesOfTheShardveil.Desktop/EchoesOfTheShardveil.Desktop.csproj

# Publish a single self-contained executable for macOS on Apple Silicon
dotnet publish EchoesOfTheShardveil.Desktop/EchoesOfTheShardveil.Desktop.csproj -c Release -r osx-arm64 --self-contained

# Run the console hot-seat duel from the published build
EchoesOfTheShardveil.Desktop/bin/Release/net11.0/osx-arm64/publish/EchoesOfTheShardveil.Desktop --alpha-duel
```

The scratch pad's commands also include `dotnet test`, but there is no test project yet, so that step does nothing.

Publish settings in the Desktop project:

* `PublishSingleFile` and `PublishSelfContained` produce one executable that needs no installed .NET runtime.
* If no runtime identifier is given, **publishing** defaults to the current machine's (`osx-arm64` on the dev Mac). Ordinary builds stay runtime-neutral. Pass `-r` to publish for another platform.
* `IncludeAllContentForSelfExtract` is on because MonoGame's SDL loader needs the managed assembly and native library on disk.
* Debug symbols are not copied to the publish folder, and the related `NETSDK1244` warning is shown as a message.

## Embedded assets

Every image, the font sheets and the music are `EmbeddedResource` items with short logical names (for example `Board-Font.png`). `CardGame.OpenEmbeddedAsset(name)` opens them and throws a clear error if one is missing. This keeps the single-file executable truly single-file and removed the need for a `Content` directory at run time. See [art-and-audio](../art-and-audio/README.md).

## macOS quit handling

MonoGame reports the window's close button and the application's Quit command (macOS menu bar or Cmd+Q) as the same "exiting" event. The game wants different behavior: the close button should ask "Exit?" or "Forfeit?", but Quit should just quit.

`QuitWatcher.cs` solves this by registering an SDL event watch (`SDL_AddEventWatch`) through `NativeLibrary`:

* SDL sends a **window close** event and then a **quit** event when the close button is pressed.
* SDL sends only a **quit** event when the application is quit from elsewhere.

So a quit with no window close right before it is an application quit. `CardGame.OnExiting` asks `QuitWatcher.TakeApplicationQuit()` first and exits straight away if it is true. If SDL can't be loaded, the watcher does nothing and every close asks first.

## High-DPI windows

On Retina displays the window's size in points differs from its size in pixels. `DesktopDrawable.cs` asks SDL for the window size (`SDL_GetWindowSize`) and the drawable size (`SDL_GL_GetDrawableSize`) every frame, resizes the back buffer to the real pixel size, and returns the client size so mouse positions can be mapped into board and menu coordinates correctly.

## The rename

On November 26, 2026 the game was renamed from **TheCards** to **Echoes of the Veil**, then to **Echoes of the Shardveil** because the first name was taken. Commit `121a24c` updated:

* project folders, project files and the solution (`TheCards.slnx` became `EchoesOfTheShardveil.slnx`),
* every namespace (`TheCards.Core` became `EchoesOfTheShardveil.Core`, and so on),
* the icon files, the window titles, and the console banner,
* the VS Code workspace and the design PDF's file name,
* the build commands in the scratch pad.

The repository folder on disk was renamed to match. The GitHub repository is still called `TheCards`, and the design plan's own text still says "TheCards".

A backup archive of the pre-rename tree was made first and removed again in `4127ed6` once the rename was confirmed.

## Repository housekeeping

The repository started from the APrettyCoolProgram repository template. So far:

* the `project/Project01/` placeholder was removed (`3638b63`),
* the unused Collection, Projects and simple README templates were removed (`4127ed6`).

Still from the template: the root `README.md` (the template's own instructions), `SourceCode-README.md` (meant to become the README), `%ProjectName%` placeholders in `AGENTS.md` and several `docs/` files, and the template logo. `.github/development/Development-Roadmap.md` has one item: fix the `AGENTS.md` links.

***

❰ [Making of alpha.02](../README.md) ❬ Tooling
