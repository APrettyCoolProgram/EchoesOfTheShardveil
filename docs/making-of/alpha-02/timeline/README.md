❰ [Making of alpha.02](../README.md) ❬ Timeline

# Timeline

Alpha.02 was built over about 28 hours, from the afternoon of November 25 to the evening of November 26, 2026. Times below are US Eastern, as recorded in the commits.

## November 25, 2026

### 3:12 PM: Initial commit (`c522458`)

The repository was created from the APrettyCoolProgram repository template: `.github/` resource folders, a `docs/` set (changelog, roadmap, release notes, manual and so on), an `AGENTS.md`, the Apache 2.0 license, and three alternative READMEs for collection, project and source-code repositories. No game code yet.

### 4:23 PM: MonoGame desktop startup screen (`77ccf3d`)

The first code. `src/` gained the solution (`TheCards.slnx`), a `Directory.Build.props`, a near-empty Core project with a `CardDefinition` record, and a Desktop project that opened a window showing the painted mural (`MainUI-Mural.png`) with about 600 lines of procedural animation in `MuralEffects.cs` and looping theme music (`MainUI-Theme.wav`). Single-file publishing was set up from the start, and the build and run commands were written into the scratch pad.

### 4:42 PM: Title artwork (`51c75ce`)

`MainUI-Title.png` was embedded and drawn centered over the mural, scaling with it.

### 5:04 PM: Menu artwork and mural effect fixes (`739e736`)

`MainUI-Menu.png` added the menu labels. Animated effects were clipped to the mural so they no longer spilled past its edges, and overlapping animation masks were separated.

### 7:21 PM: Alpha.02 playable duels (`2dc1e44`)

The big one: about 3,500 new lines in a single commit.

* **Rules engine.** `Duel.cs` (about 590 lines), `DuelState.cs` and `StarterDecks.cs` in Core implement the whole design plan. `CardDefinition` grew types, keywords and data-driven abilities.
* **The board.** `BoardUI.cs` (about 1,160 lines at the time), `BoardScreen.cs`, `BoardTutorial.cs`, `CardRulesText.cs` and `SinglePlayerOpponent.cs` made the duel playable against the computer.
* **One game, several screens.** The app had been two `Game` instances run one after another (menu, then board). It became one `CardGame` that owns the window and audio and switches between `IScreen` implementations, so music plays continuously from menu to board.
* **Window controls.** `WindowControls.cs` added the exit and sound icons and a confirmation dialog.
* **Console mode.** `AlphaDuelConsole.cs` added the `--alpha-duel` hot-seat playtest.
* **Build clean-up.** Shared settings moved into `src/Directory.Build.props`, `src/global.json` pinned SDK 11.0.100, and the Core project file shrank to a few lines.
* **Supporting pieces.** A bitmap font (`Board-Font.png`), the Hero portrait (`Board-Hero-Knight.png`), the 0 to 20 health badge atlas (`Board-Hero-Health.png`), an app icon, the burn-in title reveal (`TitleReveal.cs`, `TitleBurn.cs`), and window and layout helpers (`DesktopDrawable.cs`, `ScreenLayout.cs`, `BoardViewport.cs`, `CircleTexture.cs`).
* The design plan PDF was committed under `.github/development/alpha/alpha.02/`.

### 7:59 PM: Board counters and quit handling (`98d1b11`)

* The Hero portrait moved to the lower-left corner and the hand was centered in the space to its right. The "YOUR HAND" label and hint text above the hand were removed.
* Badges on the portrait rim show the Well (12 o'clock) and Health (6 o'clock), each with a hover tooltip.
* Card-shaped counters for **Removed**, **Discard** and **Deck** were added beside the portrait, drawn with a new Inter-style digit sheet (`Counter-Digits.png`).
* `QuitWatcher.cs` lets Quit from the macOS menu bar, or Cmd+Q, exit immediately, while the window's close button still asks first.
* The tutorial text was updated to match.

### 8:26 PM: Duel board redesign with character artwork (`b8b6b0f`)

The opponent got its own portrait (`Board-Opponent-Lich-King.png`) with the same Well and Health badges and counters, and can be clicked as an attack target. Each side got a painted playmat (`Board-Hero-Knight-Playmat.jpg`, `Board-Opponent-Lich-King-Playmat.jpg`). The status display was simplified, the panel backgrounds behind each side's rows were removed so the playmats show through, and the tutorial and rules text switched to the player-facing terms **Hero** and **Opponent**. The app icons were refreshed.

## November 26, 2026

### 7:11 AM: Ultrawide layouts (`d7f63e4`)

The board now grows to use the full window width instead of letterboxing. On the main menu, displays at 21:9 or wider get extra painted side scenes (`MainUI-SideScenes.png`) blended into both sides of the mural, animated by the new `SideSceneEffects.cs`.

### 7:30 AM: Ultrawide mural animations (`f507225`)

More animation in the side scenes: forest mist, a rippling tree, flickering city lights, canister bubbles, and refined cape and leaf motion. The mural artwork was updated to match.

### 9:37 AM: Template scaffold removed (`3638b63`)

The template's `project/Project01/` placeholder (about 1,400 lines of boilerplate) was deleted, since this is a source-code repository.

### 6:54 PM: Rename to Echoes of the Shardveil (`121a24c`)

Every project, namespace, file, asset name, window title and build command moved from `TheCards` to `EchoesOfTheShardveil` (via a brief stop at `EchoesOfTheVeil`, whose name was taken). The solution became `EchoesOfTheShardveil.slnx`. A backup archive of the pre-rename tree was made first. See [tooling](../tooling/README.md#the-rename).

### 7:00 PM: Obsolete templates removed (`4127ed6`)

The unused collection, project and simple README templates, the pre-rename backup archive, and a stray scratch pad were deleted. This is the state documented as **0.0-alpha.02**.

***

❰ [Making of alpha.02](../README.md) ❬ Timeline
