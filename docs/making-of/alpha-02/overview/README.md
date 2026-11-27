❰ [Making of alpha.02](../README.md) ❬ Overview

# Overview

## What the game is

Echoes of the Shardveil is a digital, one-on-one collectible card duel. Each player has a Leader (shown in the game as the **Hero**) with 20 life, a 40-card deck, and a single resource zone called **the Well**. You win by bringing the enemy Leader to 0, or by making your opponent draw from an empty deck.

The game started life as **TheCards**. It was renamed to **Echoes of the Veil** and then to **Echoes of the Shardveil** on November 26, 2026, because the first new name was already taken. The GitHub repository itself is still named `TheCards`.

## The design plan

The rules come from the *Starter Game Design Plan* (November 25, 2026), stored in [.github/development/alpha/alpha.02/](<../../../../.github/development/alpha/alpha.02/>). Its five design pillars are:

1. **Learn in 5 minutes, play in 15.** Three card types, five phases, six keywords.
2. **One real decision every turn.** Any card can become energy, so every card is either a play now or power for later.
3. **Defense matters on your opponent's turn.** Units held back can intercept attacks.
4. **Digital-first.** The client does the bookkeeping (damage, counters, hidden cards, randomness).
5. **Every v1 rule is a hook.** Reserved fields and spare board space leave room for colors, instant speed, traps and more.

The plan openly borrows from other games: the Well blends the Duel Masters mana zone and the Lorcana inkwell, the Leader with a life total comes from Hearthstone and Flesh and Blood, exhausting to act comes from MTG tapping and Lorcana exerting, the five-unit board limit echoes the Pokémon bench, and the second player's **Spark** plays the role of Hearthstone's Coin.

It ends with a clear next step: build the ten sample cards into two test decks and play ten or more games to tune Vitality, the Well cap, and the stat budget. Alpha.02 is that prototype.

## What alpha.02 delivers

* **A complete rules engine** for the v1 design: setup and mulligan, the five-phase turn, the Well and Spark, Units, Spells and Relics, combat with interception, all six keywords, the Arrival and Last Breath triggers, the hand limit, and every win, loss and draw condition. See [gameplay](../gameplay/README.md).
* **Two 40-card starter decks** built from a 14-card test pool (the plan's ten sample cards plus four fillers).
* **A single-player duel** against a simple computer opponent, on a 1280 x 720 board that scales to any window, with drag-and-drop play, a card inspector, hover tooltips, and a 13-page rules tutorial. See [ui](../ui/README.md).
* **An animated main menu** with a painted mural, a burn-in title reveal, looping theme music, and extra animated side scenes on ultrawide displays. See [art-and-audio](../art-and-audio/README.md).
* **A console hot-seat mode** (`--alpha-duel`) for two people to playtest the rules without graphics.
* **A tidy build**: shared project settings, a pinned SDK, single-file self-contained publishing, and all assets embedded in the executable. See [tooling](../tooling/README.md).

## What alpha.02 does not include yet

There are no automated tests, Leaders have no abilities, the Removed zone is shown but unused, and the computer opponent is deliberately basic. The full list is in [status](../status/README.md).

***

❰ [Making of alpha.02](../README.md) ❬ Overview
