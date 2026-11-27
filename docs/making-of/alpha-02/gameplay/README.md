❰ [Making of alpha.02](../README.md) ❬ Gameplay

# Gameplay

This page describes the rules **as the code implements them** in alpha.02, then lists where the implementation differs from, or goes beyond, the design plan. The rules live in `src/EchoesOfTheShardveil.Core/Duel.cs`.

| CONTENTS |
|:--|
| [Terms](#terms) |
| [Limits](#limits) |
| [Setup](#setup) |
| [The turn](#the-turn) |
| [Energy: the Well and Spark](#energy-the-well-and-spark) |
| [Playing cards](#playing-cards) |
| [Combat](#combat) |
| [Keywords and triggers](#keywords-and-triggers) |
| [Winning and losing](#winning-and-losing) |
| [The starter decks](#the-starter-decks) |
| [Design plan vs. implementation](#design-plan-vs-implementation) |

## Terms

The design plan and the code use **Leader** and **Vitality**. The player-facing text in the desktop app (tutorial, tooltips, card text) uses **Hero** for your Leader, **Opponent** for theirs, and **Health** for Vitality. The console mode still says Leader.

## Limits

All limits are constants on `Duel` and match the design plan.

| Limit | Value | Constant |
|:--|:--|:--|
| Leader health | 20 | `StartingHealth` |
| Deck size | exactly 40 | `DeckSize` |
| Copies per card name | 3 | checked in `ValidateDeck` |
| Opening hand | 5 | `OpeningHandSize` |
| Hand limit (end of turn) | 10 | `HandLimit` |
| Unit slots | 5 | `UnitSlots` |
| Relic slots | 2 | `RelicSlots` |
| Well shards | 10 | `WellLimit` |
| Card and ability cost | 0 to 10 | checked in `ValidateDeck` |

## Setup

1. Both decks are validated: exactly 40 Unit, Spell or Relic cards, costs from 0 to 10, non-negative stats and amounts, Units with at least 1 Health, and no more than three of a name (case-insensitive).
2. A random player goes first. The other player gets the **Spark**.
3. Each deck is shuffled and each player draws five cards.
4. **Mulligan, once.** Each player picks any number of hand cards. They go to the bottom of the deck in random order, and the player draws that many. Choosing none keeps the hand. In single player, the computer always keeps its hand.
5. When both players have chosen, the duel moves to the first player's Ready phase.

The constructor accepts an optional seed, so a duel can be replayed exactly. Nothing uses that yet, but it is the hook for tests.

## The turn

The phases are `Ready`, `Draw`, `Main`, `Battle`, `End` (plus `Mulligan` before the first turn and `Finished` after the last). The active player calls `Advance` to leave each phase.

| Phase | What happens |
|:--|:--|
| Ready | All your shards become ready, your Units ready, your once-per-turn Well feed resets, and "start of your turn" effects resolve. |
| Draw | Draw one card. The first player skips this on turn 1. |
| Main | In any order: feed the Well once, use your Spark, play cards. |
| Battle | Declare attacks one at a time, or skip. You can't leave Battle while an attack is waiting for the defender. |
| End | "End of your turn" effects resolve. If you hold more than 10 cards you must discard down to 10 before the turn can end. Unspent and temporary energy is lost, and play passes. |

The desktop app passes through Ready and Draw automatically, so the player only ever acts in Mulligan, Main, Battle and End.

## Energy: the Well and Spark

* **Feeding the Well.** Once per turn, during Main, put any card from your hand into the Well face down. It becomes a shard and is **ready immediately**, so it gives 1 energy this turn as well as every later turn. You can't feed a full Well (10 shards). In the app, you feed the Well by dragging a card onto your Hero portrait.
* **Paying.** Each ready shard pays 1 energy. Temporary energy is spent first.
* **Refilling.** All shards ready again at your next Ready phase. Energy does not carry over.
* **Spark.** The second player can spend it once per duel, during Main, for 1 temporary energy that lasts until the end of that turn.

## Playing cards

During your Main phase, play any card you can pay for:

* **Units** go into an open Unit slot. A full row means no more Units.
* **Relics** go into an open Relic slot and stay. A full row means no more Relics.
* **Spells** go to your discard pile and resolve their effect.

Card effects are data, not code. Each `CardAbility` is a **trigger** + a **target** + an **action** with an amount, exactly as the design plan recommended:

| Triggers | Targets | Actions |
|:--|:--|:--|
| `Spell`, `Arrival`, `LastBreath`, `StartTurn`, `EndTurn`, `Activated` | `Controller`, `ChosenUnit`, `FriendlyUnit`, `RandomFriendlyUnit` | `Damage`, `HealLeader`, `Draw`, `Ready`, `IncreasePower` |

If a played card's Spell or Arrival abilities need a chosen Unit, the play must name a legal target, so a targeted Spell can't be played when there is nothing to target. `Duel.LegalPlays` lists every legal slot and target for a hand card without changing the duel, which is what the board uses to highlight drop zones.

## Combat

Each attack follows the design plan's sequence:

1. **Declare.** Pick one of your Units that is ready, hasn't attacked this turn, and has been in play since before this turn (unless it has Rush). It exhausts, unless it is Vigilant.
2. **Target.** The enemy Leader or any enemy Unit.
3. **Intercept.** The defender may redirect the attack to one of their Units that can intercept: it must be ready (or have Sentinel), must not have intercepted already this turn, and must be Elusive if the attacker is Elusive. Intercepting exhausts it.
4. **Damage.** The attacker and a Unit target deal damage equal to their Power to each other at the same time. A Leader deals no damage back.
5. **Cleanup.** Every Unit whose damage meets or exceeds its Health is destroyed at once and goes to its owner's discard pile. Then all of their Last Breath effects resolve. If those effects destroy more Units, cleanup repeats. Damage on survivors stays.

Each Unit can attack at most once per turn, Vigilant or not, and intercept at most once per turn.

## Keywords and triggers

| Keyword | Effect in the code |
|:--|:--|
| Rush | Can attack the turn it enters play. |
| Vigilant | Doesn't exhaust when it attacks, so it can still intercept. |
| Sentinel | Can intercept while exhausted. |
| Elusive | Only Elusive Units can intercept its attacks. |
| Pierce | Combat damage beyond the target Unit's remaining Health hits the enemy Leader. This works for an intercepting or defending Unit too. |
| Drain | The controller's Leader heals by the damage this Unit actually dealt (to a Unit and, with Pierce, to a Leader), up to 20. A Leader at 0 can't be healed. |

| Trigger | When it fires |
|:--|:--|
| Arrival | When the card enters play. |
| Last Breath | When the Unit is destroyed. |
| Start of turn / End of turn | During the controller's Ready or End phase, for Units and Relics in play. |
| Activated | Paid with energy during Main through `Duel.Activate`. No alpha.02 card uses this yet. |

## Winning and losing

A player loses when their Leader reaches 0 or when they have to draw from an empty deck. If both happen together, the duel is a draw. The outcome is checked after every phase change, combat and effect, and the duel moves to `Finished`.

## The starter decks

`StarterDecks.Create` builds both decks from one 14-card pool: the ten sample cards from the design plan plus four simple Units (Gate Watch, Mire Runner, Hollow Bat and Rampart Golem). Hollow Bat was added so Drain has a card. Each deck has three copies of twelve cards and two copies of two, for 40 cards.

| Card | Type | Cost | Power / Health | Text | Your deck | Opponent's deck |
|:--|:--|:--:|:--:|:--|:--:|:--:|
| Ember Pup | Unit, Beast | 1 | 2/1 | Rush | 3 | 3 |
| Gate Watch | Unit, Knight | 1 | 1/2 | | 3 | 3 |
| Lantern Squire | Unit, Knight | 2 | 2/3 | Sentinel | 3 | 2 |
| Mistwing Scout | Unit, Bird | 2 | 2/2 | Elusive | 2 | 3 |
| Mire Runner | Unit, Beast | 2 | 3/2 | | 3 | 3 |
| Bramble Warden | Unit, Plant | 3 | 2/4 | Arrival: Restore 3 Health to your Hero | 3 | 2 |
| Gravebloom | Unit, Spirit | 3 | 3/2 | Last Breath: Draw a card | 3 | 3 |
| Hollow Bat | Unit, Beast | 3 | 3/3 | Drain | 3 | 3 |
| Wandering Oathsworn | Unit, Knight | 4 | 4/4 | Vigilant | 3 | 3 |
| Rampart Golem | Unit, Construct | 4 | 4/5 | | 3 | 3 |
| Tusked Charger | Unit, Beast | 5 | 5/4 | Pierce | 2 | 3 |
| Spark Bolt | Spell | 2 | | Deal 3 damage to a chosen Unit | 3 | 3 |
| Second Wind | Spell | 1 | | Ready a friendly Unit. Draw a card | 3 | 3 |
| Beacon of Tides | Relic | 3 | | Start of your turn: a random friendly Unit gets +1 Power permanently | 3 | 3 |

## Design plan vs. implementation

What matches: the limits, the turn structure, the Well and Spark, card types, combat, all six keywords, both trigger words, the win conditions, and the data-driven card model with a blank `Aspect` field and a `Subtype` field reserved for later sets.

Where alpha.02 differs or is incomplete:

* **Leaders are placeholders.** The engine creates "Leader One" and "Leader Two" itself. They have no abilities, as the plan intends for v1, and the Hero and Opponent art is purely cosmetic.
* **New Well shards are usable at once.** The plan says shards ready at the start of your turn. The code readies a new shard immediately, so feeding the Well gives energy on the same turn. The tutorial describes this behavior.
* **Relics don't exhaust.** The plan's Ready phase readies "shards, units and relics", but Relics have no exhausted state because no Relic has an ability that would exhaust it.
* **Activated abilities have no per-turn limit** and no card or UI uses them yet.
* **Spark Bolt can target your own Units.** `ChosenUnit` means any Unit on either side.
* **No shared Field slot** on the battle line yet. The plan leaves it empty in v1 anyway.
* **The Removed zone** is drawn on the board with a counter that always shows 0. The engine has no such zone.
* **Extra cards.** The four filler Units are not in the plan. They make the 14-card pool for two 40-card decks.

***

❰ [Making of alpha.02](../README.md) ❬ Gameplay
