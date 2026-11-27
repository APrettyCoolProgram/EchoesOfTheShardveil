❰ [Making of alpha.02](../README.md) ❬ Status

# Status at 0.0-alpha.02

## What works

* A full single-player duel against the computer, from mulligan to win, loss or draw, with a rematch button.
* Every rule in the v1 design plan except those listed below.
* The tutorial, card inspector, tooltips and drag-and-drop play.
* The animated main menu, including ultrawide displays.
* The console hot-seat mode for two-player playtesting.
* Single-file, self-contained publishing for macOS on Apple Silicon (other platforms with `-r`).

## Code review findings and follow-up

A review of the codebase found a clean separation between the rules engine and the UI, along with the issues below. Their status at alpha.02:

| Finding | Status |
|:--|:--|
| Two `Game` instances run one after another | **Done.** One `CardGame` with screens. |
| Repeated settings in each project file, no pinned SDK | **Done.** `Directory.Build.props` and `global.json`. |
| No automated tests | **Open.** The engine is deterministic when seeded, which makes it easy to test. |
| Card text on the board is hand-written per action (`BoardUI.Describe`), separate from `CardRulesText` | **Open.** For example, the board always says "Start: +1 Power", whatever the amount. |
| The computer opponent calls the engine without catching its exceptions | **Open.** The human player's input paths do catch them. |
| Activated abilities have no once-per-turn limit | **Open.** No card uses them yet. |
| An optional Arrival target can't be skipped | **Open.** No current card has a targeted Arrival. |
| The computer opponent is very simple | **Open.** See below. |

## Known gaps

* **No tests.** There is no test project.
* **Leaders.** Placeholder Leaders with no abilities; the Hero and Opponent art is cosmetic.
* **Removed zone.** Shown on the board, always 0, not in the engine.
* **Menu.** Settings, Deckbuilder and Network play are drawn but do nothing.
* **Decks.** Both players use fixed starter decks from one test pool. There is no deck building.
* **Computer opponent.** It always keeps its hand, feeds its most expensive card, plays its most expensive affordable card at the first target it finds, always attacks your Hero, and always intercepts with its first eligible Unit.
* **Design plan items not built.** The shared Field slot, and everything in the plan's "extension hooks" (Aspects, Leader abilities, instant speed, traps, a two-row board, evolution, equipment, alternate win conditions).
* **Template leftovers.** See [tooling](../tooling/README.md#repository-housekeeping).

## Candidate next steps

Roughly in the order the design plan and the review suggest:

1. **Playtest and tune.** The plan's stated next step: play 10 or more games to tune Vitality, the Well cap and the stat budget. The console mode and seeded duels help here.
2. **Add a Core test project.** Cover setup, each phase, each keyword, simultaneous cleanup and the win conditions, using fixed seeds.
3. **One source for card text.** Draw the board's card summaries from `CardRulesText`.
4. **A smarter opponent.** Choose attack targets, keep useful cards out of the Well, and decide when to intercept.
5. **Leader abilities and Aspects**, the first extension hooks the plan recommends.
6. **Finish the menu**: Settings first, then Deckbuilder.

***

❰ [Making of alpha.02](../README.md) ❬ Status
