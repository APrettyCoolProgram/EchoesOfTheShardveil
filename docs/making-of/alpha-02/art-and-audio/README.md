❰ [Making of alpha.02](../README.md) ❬ Art and audio

# Art and audio

All assets live in `src/EchoesOfTheShardveil.Desktop/Content/` and are compiled into the executable as **embedded resources** (listed in `EchoesOfTheShardveil.Desktop.csproj`). They are loaded at run time with `Texture2D.FromStream` and `SoundEffect.FromStream`. There is no MonoGame content pipeline.

## Asset list

| File | Size (px) | Used for |
|:--|:--|:--|
| `MainUI-Mural.png` | 1672 x 941 | The main menu painting |
| `MainUI-SideScenes.png` | 1024 x 1536 | Two side panels for ultrawide menus (left half: haunted forest and ruins; right half: fairy path and mushrooms) |
| `MainUI-Title.png` | 480 x 232 | The game's wordmark |
| `MainUI-Menu.png` | 1672 x 122 | The menu bar: Settings, Deckbuilder, Single player, Network play |
| `MainUI-Theme.wav` | | The looping theme music |
| `Board-Hero-Knight.png` | 256 x 256 | Your Hero portrait |
| `Board-Hero-Knight-Playmat.jpg` | | Your half of the board |
| `Board-Opponent-Lich-King.png` | 256 x 256 | The Opponent portrait |
| `Board-Opponent-Lich-King-Playmat.jpg` | | The Opponent's half of the board |
| `Board-Hero-Health.png` | 648 x 432 | Round number badges 0 to 20 (6 columns, 21 cells) for Well and Health |
| `Counter-Digits.png` | 1280 x 192 | One row of digits 0 to 9 for the card-shaped counters |
| `Board-Font.png` | 352 x 192 | ASCII glyph sheet for all other text |
| `EchoesOfTheShardveil.ico` / `.bmp` | | Application icon (the `.bmp` is embedded as `Icon.bmp` for the window icon) |

## The main menu mural

The mural is one painting that sweeps across genres from left to right: a knight and a wizard facing a fire-breathing dragon among castle ruins, a werewolf and a vampire under a gothic cathedral and full moon, a flying superheroine and a green-glowing villain over a burning city, and a space battle above a planet. It fits the game's idea of "echoes" from many worlds.

None of the movement is in the image. `MuralEffects.cs` (about 700 lines) animates it procedurally, in the mural's own pixel coordinates, so the effects stay locked to the art at any window size:

* drifting clouds and mist layers
* the dragon's fire and rising embers
* the wizard's swirling magic
* lightning arcs from the villain's backpack and hand
* billowing capes and a waving banner
* glowing windows, energy trails, flame, fabric and fog, revealed through a mask built from the painting itself
* bubbles rising in the villain's canisters
* pulsing blue starship engines and red laser fire

All effects are drawn with additive blending and clipped (scissored) to the mural, so they never spill past its edges. A note in [.github/development/working/](../../../../.github/development/working/README.md) lists the next ideas: stop the galaxy's animation, twinkle individual stars, and add more animated windows across the city.

## Ultrawide side scenes

On displays 21:9 or wider, the menu shows the mural whole and fills each side with half of `MainUI-SideScenes.png`, blended into the mural over a 48-step fade. `SideSceneEffects.cs` animates them too:

* **Left (forest):** the old tree ripples, mist drifts around the ruins, ghostly lights glow, and leaves tumble along the path.
* **Right (fairy realm):** warm windows and street lights flicker in the distant city, and color sweeps around the mushroom gills without moving the caps or stems.

## The title reveal

`TitleBurn.cs` and `TitleReveal.cs` make the wordmark appear to burn through with a backlight glow over about 8.8 seconds (a 4.4-second animation played at half speed), using the wordmark's own pixels as the shape. The reveal restarts each time the menu is shown.

## Board art

* **Portraits** are framed at run time into 176-pixel circles with a thin white border by `CircleTexture.FramePortrait`, which keeps the source PNGs simple squares.
* **Playmats** are drawn behind each half of the board, anchored so the important part of the painting stays visible as the window changes shape.
* **Badges** come from the `Board-Hero-Health.png` atlas. Numbers above 20 fall back to text on a blank round badge generated in code.

### The counter digits

The Removed, Discard and Deck counters are tiny (25 x 35 pixels, the 5:7 shape of a card). Text from the general font looked blurry and off-center at that size, so they got a dedicated sheet of regular-weight sans-serif digits (Inter style) on one row. At load time `BoardUI.BuildCounterDigits`:

1. finds each digit's ink bounds in its cell,
2. shrinks every digit to 15 pixels tall with an area-average filter, and
3. rounds every digit's width to an odd number.

Because the counter's width and height are also odd, and digits are spaced 1 pixel apart, any number lands exactly in the middle of its counter with no half-pixel blur.

## Audio

`GameAudio` loads `MainUI-Theme.wav` once, loops it at 35% volume, and keeps playing from the menu onto the board because both screens share one game. The sound icon pauses and resumes it. If the machine has no audio device, the game carries on silently instead of failing.

***

❰ [Making of alpha.02](../README.md) ❬ Art and audio
