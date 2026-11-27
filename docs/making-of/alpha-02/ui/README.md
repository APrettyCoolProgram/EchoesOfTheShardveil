❰ [Making of alpha.02](../README.md) ❬ User interface

# User interface

The app has two screens, both hosted by the single `CardGame` (see [architecture](../architecture/README.md#desktop-one-game-several-screens)):

| Screen | Window title | Code |
|:--|:--|:--|
| Main menu | Echoes of the Shardveil | `MenuScreen.cs`, `MainUI.cs` |
| Duel board | Echoes of the Shardveil - Single player | `BoardScreen.cs`, `BoardUI.cs`, `BoardTutorial.cs` |

Both share the controls in the upper right corner (`WindowControls.cs`): an **exit** icon and a **sound** icon. Escape or the exit icon opens a confirmation dialog. On the menu it offers to exit the game, and on the board it offers to forfeit and return to the menu. The window can be freely resized.

| CONTENTS |
|:--|
| [Main menu](#main-menu) |
| [Duel board](#duel-board) |
| [The tutorial](#the-tutorial) |
| [How the board UI evolved](#how-the-board-ui-evolved) |

## Main menu

* The painted **mural** fills the window (cropped to cover it). On displays 21:9 or wider, the mural is shown whole and two extra painted **side scenes** fill the space on the left and right, blended into the mural's edges over a soft fade.
* The **title** plays a burn-in reveal of about 9 seconds with a backlight each time the menu is shown, including when you return from a duel.
* The **menu bar** along the bottom shows Settings, Deckbuilder, Single player and Network play. Only **Single player** works in alpha.02. Clicking it opens a new duel.
* The theme music starts with the game and keeps playing on the board. The sound icon pauses and resumes it.

Animation details are in [art-and-audio](../art-and-audio/README.md).

## Duel board

The board is laid out on a 1280 x 720 canvas. `BoardViewport` scales it uniformly to the window, and when the window is wider or taller than 16:9 the layout **grows** into the spare space instead of letterboxing. The Opponent's half is at the top, yours at the bottom, each over its own painted playmat.

### Layout

* **Portraits.** Your Hero (a knight) is a round portrait in the lower-left corner. The Opponent (a lich king) mirrors it in the upper-left corner, and you click it to attack the Opponent directly.
* **Portrait badges.** Each portrait carries two round badges on its rim: the **Well** count at 12 o'clock and **Health** at 6 o'clock. Values from 0 to 20 come from a painted badge atlas, and anything larger is drawn as text on a blank badge.
* **Card counters.** Small card-shaped counters flank each portrait: **Removed** (upper left), **Discard** (lower left) and **Deck** (lower right). The Removed counter always shows 0 because no card can remove anything yet.
* **Tooltips.** Hover any badge or counter on your portrait for a short explanation. Tooltips appear beside the portrait so they never cover it.
* **Slots.** Each side has five Unit slots in the middle and two Relic slots on the right. Empty slots are dashed outlines labeled UNIT 1 to 5 and RELIC 1 to 2.
* **Hand.** Your hand is centered along the bottom, to the right of your portrait and counters. Cards are partly tucked below the edge and lift when you hover them.
* **Action row and status.** Buttons above the hand change with the phase, and a status line shows the latest update, such as "Opponent played Spark Bolt."
* **Spark.** "SPARK READY" and "+1 ENERGY" labels appear next to the portrait while they apply.

### Cards

Each card shows a colored stripe for its type (**teal** Units, **pink** Spells, **gold** Relics), its type and cost ("UNIT 2"), its name, its keywords or a short ability summary, and for Units, **Power / remaining Health**. Units on the board are labeled **ATTACK** or **BLOCK** when they can act now and **USED** when exhausted. A gold outline marks a selection or a legal target.

**Right-click** any card to open the inspector, which shows its full rules text from `CardRulesText`. The scroll wheel scrolls long text.

### Playing a turn

| Phase | What you do | Buttons |
|:--|:--|:--|
| Mulligan | Click cards to mark them for replacement | Keep hand / Replace *n* cards |
| Main | **Drag** a card onto your portrait to feed the Well, onto a highlighted slot to play it, onto a highlighted Unit for a targeted Spell, or into **CAST SPELL** for an untargeted Spell | Use Spark, Go to battle |
| Battle | Click a Unit marked ATTACK, then an enemy Unit or the Opponent portrait | End battle |
| Defending | Click a Unit marked BLOCK to intercept | Take the hit |
| End | Select a hand card and discard it while you hold more than 10 | Discard, End turn |
| Game over | | Play again |

**Cancel selection** appears whenever a card or attacker is selected.

While you drag, legal drop zones are outlined, and your portrait gets a teal ring (gold when hovered) if you can still feed the Well this turn. Illegal moves show the engine's message in the status line instead of crashing.

The Opponent's moves play out one step at a time, a little over a third of a second apart, so you can follow them.

## The tutorial

Each new duel opens with an optional rules walkthrough. The first panel ("Want to learn the rules?") offers **Learn the rules** or **Skip for now**, which goes straight to the mulligan. Every page has Back, Skip guide and Next. The guide has 13 pages, two of them with small illustrations:

1. Win the duel
2. Find your way around (with a board diagram)
3. Hero and counters
4. Slots and discards
5. Read a card (with a sample card)
6. Read the marks
7. Choose an opening hand
8. Follow the turn
9. Power your cards
10. Play a card
11. Use the action row
12. Attack and intercept
13. Read the keywords

The text lives in `BoardTutorial.Pages` and was updated alongside each board change so it describes the current layout.

## How the board UI evolved

1. **First playable board** (`2dc1e44`). The Hero portrait sat at the left of your row with Health on its bottom rim and a Well count inside it. The hand spanned the full width under a "YOUR HAND" label, and each side's rows sat on dark panels.
2. **Counters and layout pass** (`98d1b11`). The portrait moved to the lower-left corner, the hand label and hint were dropped, and the hand was centered in the space to the portrait's right. Well and Health became matching rim badges at 12 and 6 o'clock with tooltips. The Removed, Discard and Deck counters were added. Early versions used round badges at 9 and 8 o'clock for Removed and Discard, and these became card-shaped counters drawn with a dedicated digit sheet so the numbers stay crisp and centered at small sizes.
3. **Character artwork** (`b8b6b0f`). The Opponent got a portrait with the same badges and counters, both sides got painted playmats, the row panels were removed, and the wording moved to Hero and Opponent.
4. **Ultrawide** (`d7f63e4`). The layout grows to fill wide windows instead of letterboxing.

***

❰ [Making of alpha.02](../README.md) ❬ User interface
