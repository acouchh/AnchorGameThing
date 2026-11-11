# ARCBIT Lite - Pixel Beams & Lightning (free sampler)

Hard-edged pixel-art beams and electric effects as PNG sprite sheets, for any engine.
Free sampler of ARCBIT Vol.1 by gennai (https://gennai.itch.io). The full pack has 16 effects and 7 colour themes.

- 3 effects: laser (modular beam), zap, orb projectile (8 directions)
- Two sizes per effect (small and large), each drawn at its own resolution, never scaled
- 2 colour themes: blue and red
- Binary alpha (0 or 255), at most 4 colours per sheet, no anti-aliasing
- Every sheet comes with: the horizontal strip PNG, every frame as its own PNG, an Aseprite-style JSON
  (frame rects, durations, tags, pivot), and a GIF preview
- Godot 4.4+ project with ready SpriteFrames, a beam node and a demo scene (tested on 4.4.1 and 4.7.2)

## What the Full version adds

ARCBIT Vol.1 (paid): https://gennai.itch.io/arcbit-pixel-beams-lightning

Same file formats, two sizes per effect and the same Godot nodes as this Lite. Compared with the Lite it adds:

- 5 more modular beams: heavy beam (cannon), lightning beam, pulse beam, spiral beam (railgun) and
  tractor / heal beam, each with start, tiling body and end parts and charge, on, loop and off animations
- 8 more electric effects: lightning strike with ground flash, spark burst, shock aura, charge up and release,
  bolt projectile (8 directions), shockwave, ground crackle, tesla orb
- 5 more colour themes: purple, green, gold, white, and greyscale for your own palette swap (7 in total)
- Lightning beam body frames that can be mixed per tile, so long arcs never repeat
- 392 sprite sheets, 6,426 single-frame PNGs and 224 GIF previews in total
  (this Lite: 20 sheets, 412 frames, 12 GIFs)
- Godot SpriteFrames for all 16 effects in every size and colour, shown together in the demo scene

The Lite Godot demo has a "Full version" button in the top-right corner that opens the page above.

## Folder layout

```
sprites/<effect>/<name>.png      horizontal strip, all frames of one effect / size / colour (or beam part)
sprites/<effect>/<name>.json     Aseprite sprite-sheet JSON (array format) for that strip
frames/<effect>/<name>/<name>_NN.png   the same frames as separate files
previews/<effect>/*.gif          animated previews (scaled up, dark background)
palettes/                        colour themes as .gpl (Aseprite, GIMP, Krita) and .png swatches
manifest.csv                     one row per sheet: size, frame count, tags, timing, pivot
godot_demo/                      Godot 4.4+ project (open project.godot)
```

File names carry the frame size: `zap_32x32_blue.png` is a strip of 32x32 frames.
Beam parts are separate strips: `laser_start_32x32_blue.png`, `laser_body_32x32_blue.png`, `laser_end_32x32_blue.png`.

## Effects

| Effect | Kind | Small (frame px) | Large (frame px) | Frames | Tags | ms / frame |
|---|---|---|---|---|---|---|
| laser | beam | start 16x16, body 16x16, end 16x16 | start 32x32, body 32x32, end 32x32 | start 21, body/end 13 | charge on loop off | 50/60/70 |
| zap | fx | 32x32 | 64x64 | 8 | zap | 40/50/60/70/80 |
| orb_projectile | fx | 16x16 | 32x32 | 48 | e se s sw w nw n ne | 70 |

Timing is in milliseconds per frame (most effects run at 14-20 fps). Pivot is in pixels from the top-left of a frame:
the centre for most effects, the ground contact for `strike` and `ground_crackle`, the muzzle (start) and impact
point (end) for beams.

## Tags (animations inside one strip)

- One-shots: `strike`, `zap`, `burst`, `wave`, `release` (play once)
- Loops: `loop`, and the 8 directions of the projectiles `e se s sw w nw n ne` (screen directions, y down)
- Beams: `charge` (start part only), `on`, `loop`, `off`. The three parts have the same number of frames in
  `on`, `loop` and `off`, so frame N of the start, body and end always belong together.

The JSON follows Aseprite's "array" export: `frames[i].frame` is the rect in the strip, `frames[i].duration`
is in ms, `meta.frameTags` lists the tags, and `meta.slices[0].keys[0].pivot` is the pivot.
`meta.arcbit` adds `loop_tags`, `tile_x` (body parts) and `mix_frames`.
Tools that read Aseprite's array JSON should read these files directly. The format is checked by script; we have not
tested every third-party importer.
There are no .aseprite source files in the pack; open the PNG strip in Aseprite with File > Import Sprite Sheet.

## Using the beams (any engine)

A beam is three pieces drawn along +X, muzzle on the left:

```
[start][body][body][body]...[end]
   ^ muzzle (start pivot)       ^ impact (end pivot)
```

1. Draw the start frame so its pivot is on your muzzle.
2. Fill the space from the right edge of the start to the left edge of the end with body tiles.
3. Draw the end frame so its pivot is on the impact point.

All three use the same frame number. Body tiles always connect to each other and to the start and end.
If the gap is not a whole number of tiles you can either:
- stretch: use `round(gap / tile_width)` tiles and scale each one a little (nearest-neighbour) to fill the gap.
  Every edge still connects. This is the default in the Godot node.
- clip: cut the last tile. Looks perfect for `laser`, `heavy_beam` and `tractor_beam`; for `lightning_beam`,
  `pulse_beam` and `spiral_beam` the cut can show, so prefer stretch for those.

`lightning_beam` body frames can be mixed: give each tile a different frame (e.g. frame + 2 x tile index) so a long
arc never repeats. Any body frame connects to any other.

Vertical or reversed beams: rotate by 90 / 180 / 270 degrees (pixel-exact). Other angles work too but rotate the pixels.

Play order: `charge` (start only) -> `on` -> `loop` (repeat while firing) -> `off`.

## Godot 4.4+

Copy `godot_demo/addons/arcbit` into your project (or open `godot_demo/project.godot` to see everything).
Every effect / size / colour has a SpriteFrames resource in `addons/arcbit/frames/`.

Beam:
```gdscript
var beam := ArcbitBeam2D.new()
beam.frames = preload("res://addons/arcbit/frames/lightning_beam_large_blue.tres")
add_child(beam)
beam.length = 200.0      # or: beam.target = $Enemy (aims and stretches every frame)
beam.fire()              # charge -> on -> loop; beam.stop() plays off and emits `finished`
```
`ArcbitBeam2D` properties: `frames`, `length`, `target`, `autostart`, `use_charge`, `mix_body_frames`, `speed_scale`,
`fit` (STRETCH or CLIP). It previews in the editor.

One-shot effect:
```gdscript
ArcbitFx2D.spawn(self, preload("res://addons/arcbit/frames/zap_small_blue.tres"), hit_position)
```
`ArcbitFx2D` is an AnimatedSprite2D that puts the effect's pivot on its position and can free itself when done.
You can also use the .tres files with a plain AnimatedSprite2D (set `centered = false` and `offset = -pivot`).

Both nodes set nearest filtering on themselves. If you use the PNGs elsewhere, set
Project Settings > Rendering > Textures > Default Texture Filter to Nearest.
The demo: `1`-`2` colour, `S` small / large, `Space` restart, left click a zap, hold right click to fire a beam at the mouse.

## Unity (notes; the pack itself was tested in Godot only)

1. Import the strip PNG. Texture Type: Sprite (2D and UI), Sprite Mode: Multiple, Filter Mode: Point,
   Compression: None, Pixels Per Unit: your game's value.
2. Sprite Editor > Slice > Grid By Cell Size, using the size from the file name (e.g. 32 x 32).
   Custom pivot: x = pivot.x / width, y = 1 - pivot.y / height (pivot from the JSON or manifest.csv).
3. Select the sliced sprites of one tag and drag them into the scene to create an animation clip.
   Sample rate = 1000 / ms per frame.
4. Beams: put the start sprite at the muzzle, a SpriteRenderer with Draw Mode = Tiled (sprite Mesh Type: Full Rect)
   for the body with its width set to the gap, and the end sprite at the impact. Swap the three sprites together each frame.

## GameMaker (notes; not tested by us)

Sprite editor > Import > select the strip, or rename the file to end in `_stripN` (e.g. `zap_32x32_blue_strip8.png`)
and drag it in: GameMaker splits it into N frames. Set the sprite speed to 1000 / ms frames per second and the origin
to the pivot. Beams: `draw_sprite` the start, draw the body tiles with `draw_sprite` in a loop (or
`draw_sprite_stretched` per tile for the stretch method), then `draw_sprite` the end.

## Other engines and tools

Read frames left to right from the strip (or use the `frames/` folder), use the durations from the JSON or
manifest.csv, and scale only by whole numbers with nearest-neighbour filtering.

## Colours and palette swaps

Themes: blue and red.
Each sheet uses the 4 colours of its theme: rim, mid, bright, core.

## License

Free for commercial and non-commercial projects, no credit required. Do not redistribute the pack itself. Full text in LICENSE.txt.

## How it was made

Every frame is drawn by our own Python program, written for this pack: lightning paths, rings and beams are computed
pixel by pixel from fixed seeds and quantised to a 4-colour ramp. No generative-AI images, no stock or third-party art,
and nothing traced or copied from other packs. The code was written with the help of an AI coding assistant.
Details in CREDITS.txt.

## Rating

If this pack is useful to you, a rating on its itch page helps other Godot users find it:
https://gennai.itch.io/arcbit-lite
If something breaks or you have a question, leave a comment on the same page and I will answer.

## Changelog

- 1.0.2 - README only (nothing else changed): a short Rating section with the itch page link.
- 1.0.1 (2026-10) Added "What the Full version adds" to this README and a "Full version" button to the Godot demo. Sprites unchanged.
- 1.0 (2026-10) First release.
