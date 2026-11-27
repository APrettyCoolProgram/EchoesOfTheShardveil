❰ [Back to repository main page](../../../README.md)

# The making of Echoes of the Shardveil: 0.0-alpha.02

This folder records how **Echoes of the Shardveil** got from an empty repository template to version **0.0-alpha.02**: a playable single-player duel against a computer opponent, with an animated main menu, built in C# on MonoGame.

It covers the work from the initial commit on **November 25, 2026** through the rename and clean-up commits on **November 26, 2026** (commit `4127ed6`).

## At a glance

| | |
|:--|:--|
| Version | 0.0-alpha.02 |
| Language / runtime | C# 15 on .NET 11 (SDK 11.0.100, pinned in `src/global.json`) |
| Framework | MonoGame DesktopGL 3.8.6.1 |
| Solution | `src/EchoesOfTheShardveil.slnx` |
| Projects | `EchoesOfTheShardveil.Core` (rules engine), `EchoesOfTheShardveil.Desktop` (MonoGame app) |
| Size | about 5,100 lines of C# in 27 files |
| Playable modes | Single player against the computer; console hot-seat duel (`--alpha-duel`) |
| Former names | TheCards, then briefly Echoes of the Veil |

## Topics

| Folder | What it covers |
|:--|:--|
| [overview/](overview/README.md) | What the game is, the design plan behind it, and what alpha.02 delivers |
| [timeline/](timeline/README.md) | The commit-by-commit history, from template to alpha.02 |
| [gameplay/](gameplay/README.md) | The rules as implemented, the starter decks, and where the code differs from the design plan |
| [architecture/](architecture/README.md) | How the solution is split, the rules engine API, and the single-game screen host |
| [ui/](ui/README.md) | The main menu and the duel board, including the Hero portrait, counters and tutorial |
| [art-and-audio/](art-and-audio/README.md) | Artwork, the counter digit sheet, music, and the procedural menu animations |
| [tooling/](tooling/README.md) | Build settings, SDK pinning, publishing, macOS quit handling, and the rename |
| [status/](status/README.md) | Known gaps, review findings still open, and candidate next steps |

## Sources

These documents were written from:

* The code in [src/](../../../src).
* The git history of this repository (12 commits, `c522458` to `4127ed6`).
* The design plan PDF, [EchoesOfTheShardveil Starter Game Design Plan.pdf](<../../../.github/development/alpha/alpha.02/EchoesOfTheShardveil Starter Game Design Plan.pdf>) (dated November 25, 2026, still titled "TheCards" inside).
* The notes in [.github/development/](../../../.github/development), including the scratch pad of build commands and the working list of mural ideas.

***

❰ [Back to repository main page](../../../README.md)
